/** A graph's zoom scale and pan translation — the D3 zoom transform. */
export interface CameraTransform {
    k: number;
    x: number;
    y: number;
}

/**
 * Everything needed to restore where a reader left one stage's graph: its camera
 * (zoom + pan) and its fold state (the ids of collapsed nodes).
 */
export interface GraphViewState {
    /** The zoom/pan transform, or `null` when the graph has not been positioned yet. */
    transform: CameraTransform | null;
    /** The ids of nodes whose children are collapsed. */
    collapsed: string[];
}

/**
 * Remembers each stage's graph view state across tab switches and hot-reloads,
 * keyed by the stage's (stable, unique) title. Purely in-memory for the life of
 * the page — nothing is serialized or sent to the server, so the offline
 * single-file report stays self-contained.
 */
export class GraphCameraStore {
    private readonly byStage = new Map<string, GraphViewState>();

    /** Remembers `state` as the latest position for the stage titled `title`. */
    save(title: string, state: GraphViewState): void {
        this.byStage.set(title, state);
    }

    /** The remembered state for `title`, or `undefined` if the stage has none yet. */
    load(title: string): GraphViewState | undefined {
        return this.byStage.get(title);
    }
}
