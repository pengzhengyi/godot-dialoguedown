/** The display model produced by the .NET walk and serialized into the report. */

export interface DisplayAttribute {
    name: string;
    value: string;
}

export interface DisplayNode {
    id: string;
    label: string;
    attributes: DisplayAttribute[];
    /** The original source text this node was produced from, if known. */
    source?: string;
    /** A stable, cross-stage semantic category that drives colour. */
    category?: string;
}

export type DisplayEdgeKind = "Child" | "Reference";

export interface DisplayEdge {
    fromId: string;
    toId: string;
    kind: DisplayEdgeKind;
}

export interface Stage {
    title: string;
    nodes: DisplayNode[];
    edges: DisplayEdge[];
}

/**
 * The report payload the .NET library injects: the compiled source document and
 * each stage's display graph.
 */
export interface Report {
    /**
     * The original source document, shown in the Source tab. Absent when a single
     * graph is rendered on its own (no source to show).
     */
    source?: string;
    stages: Stage[];
    /** The document's path — present when served by the live server. */
    path?: string;
    /** True when served live: the client subscribes for hot-reload pushes. */
    live?: boolean;
}
