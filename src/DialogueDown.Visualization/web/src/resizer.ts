/** Make the detail panel draggable to resize via the divider. */
export function initResizer(): void {
    const resizer = document.getElementById("resizer");
    const detail = document.getElementById("detail");
    if (!resizer || !detail) return;

    const minWidth = 280;
    const maxWidth = 760;
    let dragging = false;

    resizer.addEventListener("mousedown", (event) => {
        dragging = true;
        document.body.style.userSelect = "none";
        event.preventDefault();
    });

    document.addEventListener("mousemove", (event) => {
        if (!dragging) return;
        const width = window.innerWidth - event.clientX;
        detail.style.flexBasis = `${Math.max(minWidth, Math.min(maxWidth, width))}px`;
    });

    document.addEventListener("mouseup", () => {
        dragging = false;
        document.body.style.userSelect = "";
    });
}
