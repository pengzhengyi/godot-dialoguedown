/**
 * When the report is served by the launcher it lives under the `/r/` mount, so a
 * `/r/` path is the signal that a "Back to launcher" control belongs in the header. A
 * report opened directly (`visualize <script>`) is served at `/` or a document sub-path
 * and gets no such control. The control is a back arrow placed immediately left of the
 * brand, on the brand's row, so a launcher-served header lines up with a directly-served
 * one (which simply omits the arrow) instead of gaining an extra top line.
 */
export function initBackToLauncher(header: HTMLElement, pathname: string): void {
    if (!pathname.startsWith("/r/")) return;

    const brand = header.querySelector(".brand");
    if (!brand) return;

    const link = document.createElement("a");
    link.className = "back-to-launcher";
    link.href = "/";
    link.title = "Back to the launcher";
    link.setAttribute("aria-label", "Back to the launcher");
    // Feather Icons (MIT) arrow-left, matching the header's other line icons.
    link.innerHTML =
        '<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor"' +
        ' stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
        '<line x1="19" y1="12" x2="5" y2="12" /><polyline points="12 19 5 12 12 5" /></svg>';
    brand.before(link);
}
