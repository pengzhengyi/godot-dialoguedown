/** The display model produced by the .NET walk and serialized into the report. */

import type { DialogueSymbols } from "./dialogue-symbols";

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
    /**
     * The node's source location as a half-open `[start, end)` character range into the
     * original document, so an edit can be spliced back into the exact source. Absent for a
     * synthetic node (no source of its own); the whole document for the document-root node.
     */
    span?: { start: number; end: number };
    /** A stable, cross-stage semantic category that drives color. */
    category?: string;
    /** A cross-link key tying the node to a semantic entity (a scene), if any. */
    entityKey?: string;
    /** The node's kind for the legend, when its label carries content (e.g. a scene title). */
    typeName?: string;
    /** A cross-link key when the node *references* an entity (a jump's scene, a speaker mention). */
    refKey?: string;
}

export type DisplayEdgeKind = "Child" | "Reference";

export interface DisplayEdge {
    fromId: string;
    toId: string;
    kind: DisplayEdgeKind;
}

/** One cell of a {@link SemanticTable}. */
export interface SemanticCell {
    text: string;
    /** Set when the cell itself is a cross-linked entity. */
    entityKey?: string;
    /** Set when the cell references another entity (a jump's resolved scene). */
    refKey?: string;
    /** A cross-stage category for color. */
    category?: string;
}

/** One row of a {@link SemanticTable}; `entityKey` names the entity the row represents. */
export interface SemanticRow {
    cells: SemanticCell[];
    entityKey?: string;
}

/** A table shown beside the scene-tree graph in the Semantic tab. */
export interface SemanticTable {
    title: string;
    columns: string[];
    rows: SemanticRow[];
    /** Shown when there are no rows. */
    emptyText: string;
}

export interface Stage {
    title: string;
    /** A one-line description of what this stage's graph shows (its tab tooltip). */
    description: string;
    nodes: DisplayNode[];
    edges: DisplayEdge[];
    /**
     * Optional tables shown beside the graph — the Semantic tab's speaker, anchor, and
     * jump-resolution tables. Absent for a plain graph stage.
     */
    tables?: SemanticTable[];
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
    /** The document's path — present when the CLI or server knows the file. */
    path?: string;
    /** How the report is shown; drives the mode badge and whether to go live. */
    mode?: VisualizationMode;
    /**
     * The semantic analyzer's resolved symbols (canonical speaker ids, merged tags,
     * validated jump targets), used to seed the Source editor's autocompletion.
     * Absent when the semantic stage did not run or produced nothing.
     */
    symbols?: DialogueSymbols;
    /**
     * The applied configuration, shown in the Config tab. Present when the report has a
     * configuration context (a CLI or served report); absent for a bare library render.
     */
    configuration?: ConfigReport;
}

/** The mode a report is shown in (mirrors the .NET `VisualizationMode`). */
export type VisualizationMode = "static" | "view" | "edit";

/** The two interactive modes of a served session, toggled in the browser (Vim-like). */
export type ServedMode = "view" | "edit";

/** One tag of a {@link ConfiguredSpeakerView}; `reserved` colors reserved names apart from custom. */
export interface ConfiguredTagView {
    name: string;
    value?: string;
    reserved: boolean;
}

/** A configured speaker shown in the Config tab: a name, an optional id, and its tags. */
export interface ConfiguredSpeakerView {
    name: string;
    id?: string;
    tags: ConfiguredTagView[];
}

/**
 * The applied configuration shown in the Config tab: the `dialogue.toml` file (when one was
 * found) and the resolved configured speakers. An absent {@link ConfigReport.file} is the
 * no-config state — the compiler used its built-in defaults.
 */
export interface ConfigReport {
    file?: { path: string; source: string };
    speakers: ConfiguredSpeakerView[];
    /** The reserved tag names the compiler recognizes (for the editor's autocompletion). */
    reservedTags?: string[];
}

/** Whether a `dialogue.toml` was found and applied (as opposed to the defaults). */
export function isConfiguredFromFile(config: ConfigReport): boolean {
    return config.file != null;
}
