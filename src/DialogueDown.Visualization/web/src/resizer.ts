/** Make the detail panel draggable to resize via the divider. */
export function initResizer(): void {
    const resizer = document.getElementById("resizer");
    const detail = document.getElementById("detail");
    if (!resizer || !detail) return;

    const minWidth = 280;
    const maxWidth = 760;
    let dragging = false;
    let rightEdge = window.innerWidth;

    resizer.addEventListener("mousedown", (event) => {
        dragging = true;
        // The detail panel's right edge is the reference for the width: with the content
        // area padded, that edge is inset from the window, so measure it rather than
        // assuming the viewport edge. (getBoundingClientRect is 0 under jsdom — fall back.)
        rightEdge = detail.getBoundingClientRect().right || window.innerWidth;
        document.body.style.userSelect = "none";
        event.preventDefault();
    });

    document.addEventListener("mousemove", (event) => {
        if (!dragging) return;
        const width = rightEdge - event.clientX;
        detail.style.flexBasis = `${Math.max(minWidth, Math.min(maxWidth, width))}px`;
    });

    document.addEventListener("mouseup", () => {
        dragging = false;
        document.body.style.userSelect = "";
    });
}
