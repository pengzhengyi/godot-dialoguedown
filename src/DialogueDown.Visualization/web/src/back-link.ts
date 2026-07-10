/**
 * When the report is served by the launcher it lives under the `/r/` mount, so a
 * `/r/` path is the signal that a "Back to launcher" control belongs in the header. A
 * report opened directly (`visualize <script>`) is served at `/` or a document sub-path
 * and gets no such control.
 */
export function initBackToLauncher(header: HTMLElement, pathname: string): void {
    if (!pathname.startsWith("/r/")) return;

    const link = document.createElement("a");
    link.className = "back-to-launcher";
    link.href = "/";
    link.textContent = "\u2190 Launcher";
    link.title = "Back to the launcher";
    header.prepend(link);
}
