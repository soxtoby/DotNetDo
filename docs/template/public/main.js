/*
 * The modern template's head emits a single <link rel="icon"> from
 * _appFaviconPath, so the apple-touch-icon has to be attached here. iOS reads
 * the head when "Add to Home Screen" runs, which is long after this executes.
 */
const appleTouchIcon = document.createElement('link')
appleTouchIcon.rel = 'apple-touch-icon'
appleTouchIcon.href = new URL('apple-touch-icon.png', import.meta.url).href
document.head.appendChild(appleTouchIcon)

export default {}
