export function isNearBottom(element, threshold) {
    return element.scrollTop + element.clientHeight >= element.scrollHeight - threshold;
}
