// Factor each zoom-in/out button press multiplies (or divides) the scale by.
export const ZOOM_STEP = 1.3;

export interface ZoomHandlers {
    onZoomIn(): void;
    onZoomOut(): void;
    onReset(): void;
}

export interface ZoomControls {
    element: HTMLElement;
    /** Reflect the current scale in the middle button (0.85 -> "85%"). */
    setRatio(scale: number): void;
}

/** The "− [ratio] +" zoom widget. +/- zoom; the middle shows the live % and resets. */
export function createZoomControls(handlers: ZoomHandlers): ZoomControls {
    const container = document.createElement("div");
    container.className = "zoom-controls";

    container.append(controlButton("−", "Zoom out", handlers.onZoomOut));
    const ratio = controlButton("100%", "Reset zoom to fit", handlers.onReset);
    ratio.classList.add("zoom-ratio");
    container.append(ratio);
    container.append(controlButton("+", "Zoom in", handlers.onZoomIn));

    return {
        element: container,
        setRatio(scale) {
            ratio.textContent = `${Math.round(scale * 100)}%`;
        },
    };
}

function controlButton(text: string, ariaLabel: string, onClick: () => void): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = text;
    button.title = ariaLabel;
    button.setAttribute("aria-label", ariaLabel);
    button.addEventListener("click", onClick);
    return button;
}
