using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

if (args.Length != 4)
    throw new ArgumentException("Usage: DocComposer <raw-api-directory> <output-directory> <families.json> <documentation.xml>");

var rawDirectory = Path.GetFullPath(args[0]);
var outputDirectory = Path.GetFullPath(args[1]);
var familyFile = Path.GetFullPath(args[2]);
var documentationFile = Path.GetFullPath(args[3]);

if (!Directory.Exists(rawDirectory))
    throw new DirectoryNotFoundException($"Raw API directory not found: {rawDirectory}");
if (!File.Exists(familyFile))
    throw new FileNotFoundException("API family definition not found.", familyFile);
if (!File.Exists(documentationFile))
    throw new FileNotFoundException("Compiler XML documentation not found.", documentationFile);
if (outputDirectory == Path.GetPathRoot(outputDirectory))
    throw new InvalidOperationException("Refusing to use a filesystem root as the generated documentation directory.");

var configuration = JsonSerializer.Deserialize<FamilyConfiguration>(
        File.ReadAllText(familyFile),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("The API family definition is empty.");

var documents = Directory
    .EnumerateFiles(rawDirectory, "*.yml", SearchOption.TopDirectoryOnly)
    .Where(path => !Path.GetFileName(path).Equals("toc.yml", StringComparison.OrdinalIgnoreCase))
    .Select(LoadApiDocument)
    .ToArray();
var byUid = documents.ToDictionary(document => document.Uid, StringComparer.Ordinal);
var facadeDocuments = documents
    .Where(document => document.Uid == "DotNetDo.Do" || document.Uid == "DotNetDo.Tools" || document.Uid.StartsWith("DotNetDo.Tools.", StringComparison.Ordinal))
    .ToArray();
var typeDocuments = documents
    .Where(document => document.CommentId?.StartsWith("T:", StringComparison.Ordinal) == true)
    .Except(facadeDocuments)
    .ToArray();

ValidateFamilies(configuration.Families, facadeDocuments, typeDocuments);

if (Directory.Exists(outputDirectory))
    Directory.Delete(outputDirectory, recursive: true);
Directory.CreateDirectory(outputDirectory);

var docsDirectory = Directory.GetParent(Path.GetDirectoryName(familyFile)!)!.FullName;
CopyAuthoredContent(docsDirectory, outputDirectory);

var typeDirectory = Path.Combine(outputDirectory, "reference", "types");
Directory.CreateDirectory(typeDirectory);

var facadeTargets = BuildFacadeTargets(configuration.Families);
facadeTargets["DotNetDo.Do"] = "reference/index.html";
facadeTargets["DotNetDo.Tools"] = "reference/tools/index.html";

var entryIndex = facadeDocuments
    .SelectMany(document => ReadMemberGroups(document).Select(group => (document, group)))
    .ToDictionary(pair => pair.group.Uid, pair => pair, StringComparer.Ordinal);
var xrefTargets = BuildXrefTargets(typeDocuments, configuration.Families, entryIndex, facadeTargets);
var externalKinds = ReadCrefKinds(documentationFile);
var unresolvedXrefs = new List<string>();

foreach (var document in typeDocuments)
{
    var family = configuration.Families.Single(candidate => Regex.IsMatch(document.Uid, candidate.TypePattern));
    var outputPath = Path.Combine(typeDirectory, Path.GetFileName(document.Path));
    var root = CloneMapping(document.Root);
    var body = GetSequence(root, "body");
    CleanType(body, document.DisplayName);
    RewriteUrls(root, url => RewriteTypeUrl(url, facadeTargets));
    RewriteXrefs(root, TypePage(document), xrefTargets, externalKinds, unresolvedXrefs);
    SetMetadata(root, "family", family.Title);
    SaveApiPage(outputPath, root);
}

foreach (var family in configuration.Families)
{
    var familyPath = Path.Combine(outputDirectory, "reference", family.Slug.Replace('/', Path.DirectorySeparatorChar) + ".yml");
    Directory.CreateDirectory(Path.GetDirectoryName(familyPath)!);
    var body = new YamlSequenceNode();
    var primaryUid = family.Facades.FirstOrDefault() ?? $"DotNetDo.Reference.{ToUidSegment(family.Title)}";
    body.Add(ApiHeading("api1", family.Title, primaryUid));
    body.Add(ScalarBlock("markdown", family.Summary));

    if (family.IntroFile is { Length: > 0 })
    {
        var introPath = Path.Combine(Path.GetDirectoryName(familyFile)!, family.IntroFile);
        var intro = File.ReadAllText(introPath).Replace("\r\n", "\n");
        intro = Regex.Replace(intro, "^# .+?\\n+", "", RegexOptions.Singleline);
        body.Add(ScalarBlock("markdown", intro.Trim()));
    }

    body.Add(ScalarBlock("h2", "Entry points"));
    string? currentFacade = family.Facades.FirstOrDefault();

    foreach (var entryUid in family.Entries)
    {
        var (source, group) = entryIndex[entryUid];
        if (currentFacade is not null && source.Uid != currentFacade && family.Facades.Contains(source.Uid, StringComparer.Ordinal))
        {
            body.Add(ApiHeading("api2", DisplayFacade(source.Uid), source.Uid));
            currentFacade = source.Uid;
        }

        var cloned = group.Nodes.Select(CloneNode).ToArray();
        QualifyEntry(cloned[0], DisplayFacade(source.Uid));
        foreach (var node in cloned)
        {
            RewriteUrls(node, url => RewriteFamilyUrl(url, family.Slug, facadeTargets));
            RewriteXrefs(node, FamilyPage(family), xrefTargets, externalKinds, unresolvedXrefs);
            body.Add(node);
        }
    }

    var relatedTypes = typeDocuments
        .Where(document => Regex.IsMatch(document.Uid, family.TypePattern))
        .OrderBy(document => document.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    body.Add(ScalarBlock("h2", "Related types"));
    var links = new YamlSequenceNode();
    foreach (var type in relatedTypes)
    {
        links.Add(new YamlMappingNode
        {
            { "text", type.DisplayName },
            { "url", $"../types/{Path.GetFileNameWithoutExtension(type.Path)}.html" }
        });
    }
    body.Add(new YamlMappingNode { { "list", links } });

    var root = new YamlMappingNode
    {
        { "title", family.Title },
        { "body", body },
        { "languageId", "csharp" },
        { "metadata", new YamlMappingNode { { "description", family.Summary } } }
    };
    SaveApiPage(familyPath, root);
}

if (unresolvedXrefs.Count > 0)
    throw new InvalidOperationException("Unresolved DotNetDo cross-references:\n" + string.Join("\n", unresolvedXrefs.Distinct().Order()));

AddTypeOwnershipToToc(outputDirectory, configuration.Families, typeDocuments);

Console.WriteLine($"Composed {configuration.Families.Count} API families and {typeDocuments.Length} canonical type pages.");

static void AddTypeOwnershipToToc(string outputDirectory, IReadOnlyList<ApiFamily> families, IReadOnlyList<ApiDocument> types)
{
    var tocPath = Path.Combine(outputDirectory, "reference", "toc.yml");
    var yaml = new YamlStream();
    using (var reader = File.OpenText(tocPath)) yaml.Load(reader);
    var root = (YamlSequenceNode)yaml.Documents.Single().RootNode;

    foreach (var family in families)
    {
        var item = FindTocItem(root, family.Slug + ".yml")
            ?? throw new InvalidOperationException($"Reference TOC has no item for API family '{family.Slug}'.");
        var children = new YamlSequenceNode();
        foreach (var type in types.Where(type => Regex.IsMatch(type.Uid, family.TypePattern)).OrderBy(type => type.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            children.Add(new YamlMappingNode
            {
                { "name", type.DisplayName },
                { "href", $"types/{Path.GetFileName(type.Path)}" }
            });
        }
        item.Children[new YamlScalarNode("items")] = children;
    }

    using var writer = new StreamWriter(tocPath, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    yaml.Save(writer, assignAnchors: false);
}

static YamlMappingNode? FindTocItem(YamlSequenceNode items, string href)
{
    foreach (var item in items.Children.OfType<YamlMappingNode>())
    {
        if (ScalarValue(item, "href") == href) return item;
        if (GetOptionalSequence(item, "items") is { } children && FindTocItem(children, href) is { } match) return match;
    }
    return null;
}

static void CopyAuthoredContent(string docsDirectory, string outputDirectory)
{
    foreach (var source in Directory.EnumerateFiles(docsDirectory, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(docsDirectory, source);
        var normalized = relative.Replace('\\', '/');
        if (normalized.StartsWith("adr/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("reference/intros/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("tools/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("template/", StringComparison.OrdinalIgnoreCase))
            continue;
        if (Path.GetExtension(source) is not (".md" or ".yml"))
            continue;

        var destination = Path.Combine(outputDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source, destination, overwrite: true);
    }
}

static void ValidateFamilies(IReadOnlyList<ApiFamily> families, IReadOnlyList<ApiDocument> facades, IReadOnlyList<ApiDocument> types)
{
    var duplicateSlugs = families.GroupBy(family => family.Slug, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
    if (duplicateSlugs.Length > 0)
        throw new InvalidOperationException($"Duplicate API family slugs: {string.Join(", ", duplicateSlugs)}");

    var entryGroups = facades.SelectMany(ReadMemberGroups).ToArray();
    var configuredEntries = families.SelectMany(family => family.Entries).ToArray();
    var duplicateEntries = configuredEntries.GroupBy(uid => uid, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
    if (duplicateEntries.Length > 0)
        throw new InvalidOperationException($"Duplicate API entry ownership: {string.Join(", ", duplicateEntries)}");

    var availableEntries = entryGroups.Select(group => group.Uid).ToHashSet(StringComparer.Ordinal);
    var unknownEntries = configuredEntries.Where(uid => !availableEntries.Contains(uid)).ToArray();
    if (unknownEntries.Length > 0)
        throw new InvalidOperationException($"Unknown API entries: {string.Join(", ", unknownEntries)}");

    var missingEntries = availableEntries.Where(uid => !configuredEntries.Contains(uid, StringComparer.Ordinal)).Order().ToArray();
    if (missingEntries.Length > 0)
        throw new InvalidOperationException($"Facade entries without a family: {string.Join(", ", missingEntries)}");

    var ownershipErrors = types
        .Select(type => (type.Uid, Owners: families.Where(family => Regex.IsMatch(type.Uid, family.TypePattern)).Select(family => family.Title).ToArray()))
        .Where(item => item.Owners.Length != 1)
        .Select(item => $"{item.Uid} [{string.Join(", ", item.Owners)}]")
        .ToArray();
    if (ownershipErrors.Length > 0)
        throw new InvalidOperationException("API types must have exactly one canonical family:\n" + string.Join("\n", ownershipErrors));
}

static Dictionary<string, string> BuildFacadeTargets(IEnumerable<ApiFamily> families)
{
    var targets = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var family in families)
        foreach (var facade in family.Facades)
            targets.Add(facade, $"reference/{family.Slug}.html");
    return targets;
}

static string RewriteTypeUrl(string url, IReadOnlyDictionary<string, string> facadeTargets)
{
    if (url == "DotNetDo.html") return "../index.html";
    var uid = Path.GetFileNameWithoutExtension(url);
    return facadeTargets.TryGetValue(uid, out var target) ? "../../" + target : url;
}

static string RewriteFamilyUrl(string url, string familySlug, IReadOnlyDictionary<string, string> facadeTargets)
{
    if (url.StartsWith("http://", StringComparison.Ordinal) || url.StartsWith("https://", StringComparison.Ordinal) || url.StartsWith('#'))
        return url;
    if (url == "DotNetDo.html") return "../index.html";

    var uid = Path.GetFileNameWithoutExtension(url);
    if (facadeTargets.TryGetValue(uid, out var target))
        return RelativeUrl($"reference/{familySlug}.html", target);
    return $"../types/{url}";
}

static string RelativeUrl(string source, string target)
{
    var sourceDirectory = Path.GetDirectoryName(source.Replace('/', Path.DirectorySeparatorChar))!;
    return Path.GetRelativePath(sourceDirectory, target.Replace('/', Path.DirectorySeparatorChar)).Replace('\\', '/');
}

static string TypePage(ApiDocument type) => $"reference/types/{Path.GetFileNameWithoutExtension(type.Path)}.html";

static string FamilyPage(ApiFamily family) => $"reference/{family.Slug}.html";

// Docfx registers no UIDs for ApiPage documents, so the composer resolves prose cross-references itself.
static Dictionary<string, XrefTarget> BuildXrefTargets(
    IEnumerable<ApiDocument> types,
    IEnumerable<ApiFamily> families,
    IReadOnlyDictionary<string, (ApiDocument document, MemberGroup group)> entryIndex,
    IReadOnlyDictionary<string, string> facadeTargets)
{
    var targets = new Dictionary<string, XrefTarget>(StringComparer.Ordinal);

    foreach (var type in types)
    {
        var page = TypePage(type);
        targets.Add(type.Uid, new XrefTarget(page, null, type.DisplayName, null));
        foreach (var member in ReadMemberGroups(type))
            targets.Add(member.Uid, new XrefTarget(page, ScalarValue(member.Nodes[0], "id"), MemberName(member), type.DisplayName));
    }

    foreach (var (uid, page) in facadeTargets)
        targets.Add(uid, new XrefTarget(page, null, DisplayFacade(uid), null));

    foreach (var family in families)
    {
        foreach (var entry in family.Entries)
        {
            var (source, group) = entryIndex[entry];
            targets.Add(entry, new XrefTarget(FamilyPage(family), ScalarValue(group.Nodes[0], "id"), $"{DisplayFacade(source.Uid)}.{MemberName(group)}", null));
        }
    }

    return targets;
}

static string MemberName(MemberGroup member)
{
    var name = ScalarValue(member.Nodes[0], "api3") ?? member.Uid;
    var parameters = name.IndexOf('(');
    return parameters < 0 ? name : name[..parameters] + "()";
}

static Dictionary<string, char> ReadCrefKinds(string documentationFile) =>
    XDocument.Load(documentationFile)
        .Descendants()
        .Attributes("cref")
        .Select(attribute => attribute.Value)
        .Where(cref => cref.Length > 2 && cref[1] == ':')
        .DistinctBy(cref => cref[2..])
        .ToDictionary(cref => cref[2..], cref => cref[0], StringComparer.Ordinal);

static void RewriteXrefs(YamlNode node, string page, IReadOnlyDictionary<string, XrefTarget> targets, IReadOnlyDictionary<string, char> externalKinds, List<string> unresolved)
{
    switch (node)
    {
        case YamlScalarNode { Value: { } value } scalar when value.Contains("<xref ", StringComparison.Ordinal):
            scalar.Value = Regex.Replace(
                value,
                """<xref href="(?<uid>[^"]*)"[^>]*?(?:/>|>(?<text>.*?)</xref>)""",
                match => XrefLink(match, page, targets, externalKinds, unresolved),
                RegexOptions.Singleline);
            break;
        case YamlMappingNode mapping:
            foreach (var pair in mapping.Children) RewriteXrefs(pair.Value, page, targets, externalKinds, unresolved);
            break;
        case YamlSequenceNode sequence:
            foreach (var child in sequence.Children) RewriteXrefs(child, page, targets, externalKinds, unresolved);
            break;
    }
}

static string XrefLink(Match match, string page, IReadOnlyDictionary<string, XrefTarget> targets, IReadOnlyDictionary<string, char> externalKinds, List<string> unresolved)
{
    var uid = WebUtility.HtmlDecode(match.Groups["uid"].Value);
    var text = match.Groups["text"].Value;

    if (targets.TryGetValue(uid, out var target))
    {
        var href = RelativeUrl(page, target.Page) + (target.Anchor is null ? "" : "#" + target.Anchor);
        var name = target.Owner is not null && target.Page != page ? $"{target.Owner}.{target.Name}" : target.Name;
        return XrefAnchor(href, text.Length > 0 ? text : WebUtility.HtmlEncode(name));
    }

    if (uid.StartsWith("DotNetDo.", StringComparison.Ordinal))
    {
        unresolved.Add($"{uid} (referenced from {page})");
        return match.Value;
    }

    var display = text.Length > 0 ? text : WebUtility.HtmlEncode(ExternalName(uid, externalKinds));
    return uid.StartsWith("System.", StringComparison.Ordinal) || uid.StartsWith("Microsoft.", StringComparison.Ordinal)
        ? XrefAnchor($"https://learn.microsoft.com/dotnet/api/{Regex.Replace(uid, @"\(.*$", "").Replace('`', '-').ToLowerInvariant()}", display)
        : display;
}

static string XrefAnchor(string href, string text) => $"""<a class="xref" href="{WebUtility.HtmlEncode(href)}">{text}</a>""";

// Types show only their name; members show their declaring type too, since the namespace boundary isn't otherwise known.
static string ExternalName(string uid, IReadOnlyDictionary<string, char> kinds)
{
    var kind = kinds.GetValueOrDefault(uid, 'T');
    var segments = Regex.Replace(uid, @"\(.*$", "").Split('.');
    var name = kind == 'T' ? segments[^1] : $"{segments[^2]}.{segments[^1]}";
    name = Regex.Replace(name, @"`+\d+", "");
    return kind == 'M' ? name + "()" : name;
}

static void CleanType(YamlSequenceNode body, string typeName)
{
    RemoveMetadataNoise(body, typeName);
    RemoveRecordNoise(body, typeName);
    SortMembers(body);
}

static void RemoveMetadataNoise(YamlSequenceNode body, string typeName)
{
    for (var index = body.Children.Count - 2; index >= 0; index--)
    {
        if (ScalarValue(body.Children[index], "h4") is not { } heading)
            continue;
        var detail = body.Children[index + 1];

        if (heading is "Inherited Members")
        {
            body.Children.RemoveAt(index + 1);
            body.Children.RemoveAt(index);
            continue;
        }

        if (heading is "Inheritance" && detail is YamlMappingNode inheritance && GetOptionalSequence(inheritance, "inheritance") is { } chain)
        {
            var meaningful = chain.Children
                .OfType<YamlMappingNode>()
                .Where(item => ScalarValue(item, "url")?.StartsWith("DotNetDo.", StringComparison.Ordinal) == true)
                .Where(item => ScalarValue(item, "text") != typeName)
                .Cast<YamlNode>()
                .ToArray();
            chain.Children.Clear();
            foreach (var item in meaningful) chain.Add(item);
            if (chain.Children.Count == 0)
            {
                body.Children.RemoveAt(index + 1);
                body.Children.RemoveAt(index);
            }
        }

        if (heading is "Implements" && detail is YamlMappingNode implements && GetOptionalSequence(implements, "list") is { } interfaces)
        {
            var meaningful = interfaces.Children
                .OfType<YamlMappingNode>()
                .Where(item => ScalarValue(item, "text") != $"IEquatable<{typeName}>")
                .Cast<YamlNode>()
                .ToArray();
            interfaces.Children.Clear();
            foreach (var item in meaningful) interfaces.Add(item);
            if (interfaces.Children.Count == 0)
            {
                body.Children.RemoveAt(index + 1);
                body.Children.RemoveAt(index);
            }
        }
    }
}

static void RemoveRecordNoise(YamlSequenceNode body, string typeName)
{
    var isRecord = body.Children.Any(node => ScalarValue(node, "code")?.Contains(" record ", StringComparison.Ordinal) == true);
    if (!isRecord) return;

    var groups = ReadGroups(body).ToArray();
    foreach (var group in groups.Reverse())
    {
        var name = ScalarValue(group.Nodes[0], "api3") ?? "";
        var documented = group.Nodes.Any(node => ScalarValue(node, "markdown") is { Length: > 0 });
        var generated = name is "EqualityContract" or "<Clone>$()" or "GetHashCode()"
            || name.StartsWith("PrintMembers(", StringComparison.Ordinal)
            || name.StartsWith("Deconstruct(", StringComparison.Ordinal)
            || name.StartsWith("operator ==(", StringComparison.Ordinal)
            || name.StartsWith("operator !=(", StringComparison.Ordinal)
            || name.StartsWith("Equals(", StringComparison.Ordinal)
            || name == $"{typeName}({typeName})";
        if (!documented && generated)
            foreach (var node in group.Nodes) body.Children.Remove(node);
    }
}

static void SortMembers(YamlSequenceNode body)
{
    var index = 0;
    while (index < body.Children.Count)
    {
        if (ScalarValue(body.Children[index], "h2") is null) { index++; continue; }
        var end = index + 1;
        while (end < body.Children.Count && ScalarValue(body.Children[end], "h2") is null) end++;
        var section = body.Children.Skip(index + 1).Take(end - index - 1).ToArray();
        var groups = ReadGroups(new YamlSequenceNode(section)).OrderBy(group => ScalarValue(group.Nodes[0], "api3"), StringComparer.OrdinalIgnoreCase).ToArray();
        if (groups.Length > 0)
        {
            for (var remove = end - 1; remove > index; remove--) body.Children.RemoveAt(remove);
            var insert = index + 1;
            foreach (var group in groups)
                foreach (var node in group.Nodes)
                    body.Children.Insert(insert++, node);
            end = insert;
        }
        index = end;
    }
}

static IEnumerable<MemberGroup> ReadMemberGroups(ApiDocument document) => ReadGroups(GetSequence(document.Root, "body"));

static IEnumerable<MemberGroup> ReadGroups(YamlSequenceNode body)
{
    for (var index = 0; index < body.Children.Count; index++)
    {
        if (body.Children[index] is not YamlMappingNode first || ScalarValue(first, "api3") is null)
            continue;
        var nodes = new List<YamlNode> { first };
        var next = index + 1;
        while (next < body.Children.Count && ScalarValue(body.Children[next], "api3") is null && ScalarValue(body.Children[next], "h2") is null && ScalarValue(body.Children[next], "api2") is null)
            nodes.Add(body.Children[next++]);
        var uid = GetUid(first) ?? throw new InvalidOperationException($"API member '{ScalarValue(first, "api3")}' has no UID.");
        yield return new MemberGroup(uid, nodes);
        index = next - 1;
    }
}

static void QualifyEntry(YamlNode node, string facade)
{
    if (node is not YamlMappingNode mapping) return;
    var key = new YamlScalarNode("api3");
    if (mapping.Children.TryGetValue(key, out var value) && value is YamlScalarNode scalar)
        scalar.Value = $"{facade}.{scalar.Value}";
}

static string DisplayFacade(string uid) => uid["DotNetDo.".Length..];

static YamlMappingNode ApiHeading(string level, string title, string uid) => new()
{
    { level, title },
    { "id", uid.Replace('.', '_').Replace('`', '_') },
    { "metadata", new YamlMappingNode { { "uid", uid } } }
};

static YamlMappingNode ScalarBlock(string key, string value) => new() { { key, new YamlScalarNode(value) { Style = YamlDotNet.Core.ScalarStyle.Literal } } };

static void RewriteUrls(YamlNode node, Func<string, string> rewrite)
{
    switch (node)
    {
        case YamlMappingNode mapping:
            foreach (var pair in mapping.Children.ToArray())
            {
                if (pair.Key is YamlScalarNode { Value: "url" } && pair.Value is YamlScalarNode scalar && scalar.Value is { } url)
                    scalar.Value = rewrite(url);
                else
                    RewriteUrls(pair.Value, rewrite);
            }
            break;
        case YamlSequenceNode sequence:
            foreach (var child in sequence.Children) RewriteUrls(child, rewrite);
            break;
    }
}

static string? GetUid(YamlMappingNode node)
{
    var metadata = GetOptionalMapping(node, "metadata");
    return metadata is null ? null : ScalarValue(metadata, "uid");
}

static string? ScalarValue(YamlNode node, string key)
{
    if (node is not YamlMappingNode mapping) return null;
    return mapping.Children.TryGetValue(new YamlScalarNode(key), out var value) ? (value as YamlScalarNode)?.Value : null;
}

static YamlSequenceNode GetSequence(YamlMappingNode node, string key) =>
    GetOptionalSequence(node, key) ?? throw new InvalidOperationException($"YAML property '{key}' is missing or is not a sequence.");

static YamlSequenceNode? GetOptionalSequence(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out var value) ? value as YamlSequenceNode : null;

static YamlMappingNode? GetOptionalMapping(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out var value) ? value as YamlMappingNode : null;

static void SetMetadata(YamlMappingNode root, string key, string value)
{
    var metadata = GetOptionalMapping(root, "metadata");
    if (metadata is null)
    {
        metadata = new YamlMappingNode();
        root.Add("metadata", metadata);
    }
    metadata.Children[new YamlScalarNode(key)] = new YamlScalarNode(value);
}

static YamlNode CloneNode(YamlNode node) => node switch
{
    YamlScalarNode scalar => new YamlScalarNode(scalar.Value) { Style = scalar.Style, Tag = scalar.Tag },
    YamlSequenceNode sequence => new YamlSequenceNode(sequence.Children.Select(CloneNode)) { Style = sequence.Style, Tag = sequence.Tag },
    YamlMappingNode mapping => CloneMapping(mapping),
    _ => throw new NotSupportedException($"Unsupported YAML node: {node.GetType().Name}")
};

static YamlMappingNode CloneMapping(YamlMappingNode mapping)
{
    var clone = new YamlMappingNode { Style = mapping.Style, Tag = mapping.Tag };
    foreach (var pair in mapping.Children) clone.Add(CloneNode(pair.Key), CloneNode(pair.Value));
    return clone;
}

static void SaveApiPage(string path, YamlMappingNode root)
{
    using var writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    writer.WriteLine("### YamlMime:ApiPage");
    new YamlStream(new YamlDocument(root)).Save(writer, assignAnchors: false);
}

static string ToUidSegment(string value) => Regex.Replace(value, "[^A-Za-z0-9]+", "");

static ApiDocument LoadApiDocument(string path)
{
    var yaml = new YamlStream();
    using var reader = File.OpenText(path);
    yaml.Load(reader);
    var root = (YamlMappingNode)yaml.Documents.Single().RootNode;
    var body = GetSequence(root, "body");
    var heading = body.Children.OfType<YamlMappingNode>().First(node => ScalarValue(node, "api1") is not null);
    var uid = GetUid(heading) ?? throw new InvalidOperationException($"API document '{path}' has no UID.");
    var commentId = GetOptionalMapping(heading, "metadata") is { } metadata ? ScalarValue(metadata, "commentId") : null;
    var displayName = Regex.Replace(ScalarValue(heading, "api1") ?? uid, "^(Class|Struct|Interface|Enum|Delegate) ", "");
    return new ApiDocument(path, root, uid, commentId, displayName);
}

sealed record MemberGroup(string Uid, IReadOnlyList<YamlNode> Nodes);

sealed record XrefTarget(string Page, string? Anchor, string Name, string? Owner);

sealed record ApiDocument(string Path, YamlMappingNode Root, string Uid, string? CommentId, string DisplayName);

sealed class FamilyConfiguration
{
    public List<ApiFamily> Families { get; init; } = [];
}

sealed class ApiFamily
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public string? IntroFile { get; init; }
    public List<string> Facades { get; init; } = [];
    public List<string> Entries { get; init; } = [];
    public required string TypePattern { get; init; }
}
