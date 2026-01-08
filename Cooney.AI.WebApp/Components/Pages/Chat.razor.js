// JavaScript module for Chat.razor - handles scrolling functionality

/**
 * Scrolls the message container to the bottom
 * @param {string} containerId - The ID of the container element
 * @param {boolean} smooth - Whether to use smooth scrolling animation
 */
export function scrollToBottom(containerId, smooth = false) {
    // Use requestAnimationFrame to ensure scroll happens after browser render
    requestAnimationFrame(() => {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn(`Container with ID "${containerId}" not found`);
            return;
        }

        if (smooth) {
            container.scrollTo({
                top: container.scrollHeight,
                behavior: 'smooth'
            });
        } else {
            // Instant scroll - prevents animation stacking during rapid updates
            container.scrollTop = container.scrollHeight;
        }
    });
}

/**
 * Checks if the user has manually scrolled up from the bottom
 * @param {string} containerId - The ID of the container element
 * @param {number} threshold - Distance in pixels from bottom to consider "scrolled up"
 * @returns {boolean} True if user is scrolled up beyond threshold
 */
export function isUserScrolledUp(containerId, threshold = 150) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.warn(`Container with ID "${containerId}" not found`);
        return false;
    }

    // Calculate how far from bottom we are
    const scrollBottom = container.scrollHeight - container.clientHeight - container.scrollTop;
    return scrollBottom > threshold;
}
