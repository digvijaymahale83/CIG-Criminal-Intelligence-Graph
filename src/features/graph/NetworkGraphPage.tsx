import React, { useEffect, useRef, useState, useCallback } from 'react';
import cytoscape, { Core } from 'cytoscape';
import {
  ZoomIn, ZoomOut, Maximize2, Search, X, ExternalLink, GitBranch, RefreshCw,
  SlidersHorizontal, Route, BarChart3, ShieldCheck, FileText, ChevronRight,
  Info, ArrowRight, CheckCircle2, AlertTriangle, Layers, GitMerge, Sparkles,
  Clock, Calendar, MapPin
} from 'lucide-react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { useTheme } from '../../hooks/useTheme';
import {
  graphService,
  GraphNodeDto,
  GraphEdgeDto,
  GraphStatisticsDto,
  GraphSearchResultDto,
  ShortestPathDto,
  RelationshipDetailDto
} from '../../services/graph/graph.service';
import {
  entityResolutionService,
  EntityMatchCandidate
} from '../../services/entity-resolution/entityResolution.service';
import {
  analyticsService,
  EntityAnalyticsProfileDto,
  GraphAnalyticalLeadDto
} from '../../services/analytics/analytics.service';
import {
  timelineService,
  TimelineEventDto
} from '../../services/timeline/timeline.service';

const TYPE_COLORS: Record<string, string> = {
  'PERSON':       '#388bfd', // Blue
  'PHONE':        '#d29922', // Gold
  'PHONE_NUMBER': '#d29922',
  'VEHICLE':      '#db6d28', // Orange
  'LOCATION':     '#2ea043', // Green
  'ORGANIZATION': '#8250df', // Purple
  'ACCOUNT':      '#cf222e', // Coral/Red
  'DEVICE':       '#1f883d', // Teal
  'EVENT':        '#bb8009', // Amber
  'DOCUMENT':     '#6e7781', // Slate
  'CASE':         '#0550ae', // Deep Cyan
};

const CANONICAL_ENTITY_TYPES = [
  'PERSON', 'PHONE', 'VEHICLE', 'LOCATION', 'ORGANIZATION', 'ACCOUNT', 'DEVICE', 'EVENT', 'DOCUMENT'
];

const CANONICAL_RELATIONSHIP_TYPES = [
  'COMMUNICATED_WITH', 'OPERATES', 'LOCATED_AT', 'ASSOCIATE_OF',
  'MEMBER_OF', 'TRANSFERRED_MONEY_TO', 'TRAVELLED_TO', 'OWNED_BY', 'TRAFFICKED'
];

export const NetworkGraphPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const highlightNodeId = searchParams.get('highlightNode') || searchParams.get('highlightEntity');
  const { activeCaseId, activeCaseNumber, activeCaseTitle } = useActiveInvestigation();
  const { resolvedTheme } = useTheme();
  const effectiveCaseId = activeCaseId || 'inv-2026-001';

  const cyRef = useRef<HTMLDivElement>(null);
  const cyInstance = useRef<Core | null>(null);

  // Graph state
  const [nodes, setNodes] = useState<GraphNodeDto[]>([]);
  const [edges, setEdges] = useState<GraphEdgeDto[]>([]);
  const [selectedNode, setSelectedNode] = useState<GraphNodeDto | null>(null);
  const [selectedEdge, setSelectedEdge] = useState<GraphEdgeDto | null>(null);
  const [edgeDetails, setEdgeDetails] = useState<RelationshipDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [expandingNeighbors, setExpandingNeighbors] = useState(false);
  const [nodeCrossCaseMatches, setNodeCrossCaseMatches] = useState<EntityMatchCandidate[]>([]);
  const [nodeAnalytics, setNodeAnalytics] = useState<EntityAnalyticsProfileDto | null>(null);
  const [nodeTimelineEvents, setNodeTimelineEvents] = useState<TimelineEventDto[]>([]);
  const [edgeTimelineEvents, setEdgeTimelineEvents] = useState<TimelineEventDto[]>([]);
  const [loadingNodeTimeline, setLoadingNodeTimeline] = useState(false);
  const [loadingEdgeTimeline, setLoadingEdgeTimeline] = useState(false);
  const [showModelLeadsOnGraph, setShowModelLeadsOnGraph] = useState(false);
  const [modelLeads, setModelLeads] = useState<GraphAnalyticalLeadDto[]>([]);

  useEffect(() => {
    if (selectedNode) {
      entityResolutionService
        .getEntityCrossCaseMatches(selectedNode.id)
        .then(setNodeCrossCaseMatches)
        .catch(() => setNodeCrossCaseMatches([]));

      analyticsService
        .getEntityProfile(selectedNode.id, effectiveCaseId)
        .then(setNodeAnalytics)
        .catch(() => setNodeAnalytics(null));

      setLoadingNodeTimeline(true);
      timelineService
        .getEntityTimeline(selectedNode.id, effectiveCaseId)
        .then(setNodeTimelineEvents)
        .catch(() => setNodeTimelineEvents([]))
        .finally(() => setLoadingNodeTimeline(false));
    } else {
      setNodeCrossCaseMatches([]);
      setNodeAnalytics(null);
      setNodeTimelineEvents([]);
    }
  }, [selectedNode?.id, effectiveCaseId]);

  useEffect(() => {
    if (selectedEdge) {
      setLoadingEdgeTimeline(true);
      timelineService
        .getRelationshipTimeline(selectedEdge.id, effectiveCaseId)
        .then(setEdgeTimelineEvents)
        .catch(() => setEdgeTimelineEvents([]))
        .finally(() => setLoadingEdgeTimeline(false));
    } else {
      setEdgeTimelineEvents([]);
    }
  }, [selectedEdge?.id, effectiveCaseId]);

  useEffect(() => {
    if (highlightNodeId && nodes.length > 0 && cyInstance.current) {
      const targetNode = nodes.find((n) => n.id === highlightNodeId);
      if (targetNode) {
        setSelectedNode(targetNode);
        setSelectedEdge(null);
        const cyNode = cyInstance.current.$(`#${highlightNodeId}`);
        if (cyNode.length > 0) {
          cyInstance.current.elements().removeClass('highlighted');
          cyNode.addClass('highlighted');
          cyInstance.current.center(cyNode);
          cyInstance.current.zoom(1.4);
        }
      }
    }
  }, [highlightNodeId, nodes]);

  useEffect(() => {
    analyticsService
      .getLeads(effectiveCaseId, 'PENDING', 0.5)
      .then(setModelLeads)
      .catch(() => setModelLeads([]));
  }, [effectiveCaseId]);

  // Filter state
  const [selectedTypes, setSelectedTypes] = useState<Set<string>>(new Set(CANONICAL_ENTITY_TYPES));
  const [selectedRelTypes, setSelectedRelTypes] = useState<Set<string>>(new Set(CANONICAL_RELATIONSHIP_TYPES));
  const [confidenceThreshold, setConfidenceThreshold] = useState<'ALL' | 'HIGH' | 'MEDIUM'>('ALL');

  // Search state
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<GraphSearchResultDto[]>([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [showSearchDropdown, setShowSearchDropdown] = useState(false);

  // Shortest Path state
  const [showPathModal, setShowPathModal] = useState(false);
  const [pathStartId, setPathStartId] = useState('');
  const [pathEndId, setPathEndId] = useState('');
  const [pathMaxHops, setPathMaxHops] = useState(4);
  const [pathResult, setPathResult] = useState<ShortestPathDto | null>(null);
  const [pathLoading, setPathLoading] = useState(false);

  // Statistics modal state
  const [showStatsModal, setShowStatsModal] = useState(false);
  const [statistics, setStatistics] = useState<GraphStatisticsDto | null>(null);
  const [statsLoading, setStatsLoading] = useState(false);

  // Load Graph Data
  const loadGraph = useCallback(async () => {
    setLoading(true);
    setSelectedNode(null);
    setSelectedEdge(null);
    setEdgeDetails(null);

    try {
      const data = await graphService.getCaseGraph(effectiveCaseId, {
        limit: 250,
      });

      setNodes(data.nodes);
      setEdges(data.edges);
    } catch (err) {
      console.error('Failed to load investigation graph:', err);
    } finally {
      setLoading(false);
    }
  }, [effectiveCaseId]);

  useEffect(() => {
    loadGraph();
  }, [loadGraph]);

  // Load Edge Details when edge selected
  useEffect(() => {
    if (!selectedEdge) {
      setEdgeDetails(null);
      return;
    }

    const fetchDetails = async () => {
      try {
        const details = await graphService.getRelationshipDetails(selectedEdge.id, effectiveCaseId);
        setEdgeDetails(details);
      } catch (err) {
        console.error('Failed to load edge details:', err);
      }
    };

    fetchDetails();
  }, [selectedEdge, effectiveCaseId]);

  // Filter elements
  const getVisibleElements = useCallback(() => {
    const visibleNodes = nodes.filter((n) => {
      const typeMatch = selectedTypes.has(n.type.toUpperCase());
      return typeMatch;
    });

    const visibleNodeIds = new Set(visibleNodes.map((n) => n.id));

    const visibleEdges = edges.filter((e) => {
      const endpointsMatch = visibleNodeIds.has(e.source) && visibleNodeIds.has(e.target);
      const relTypeMatch = selectedRelTypes.has(e.type.toUpperCase());
      const confMatch =
        confidenceThreshold === 'ALL' ||
        (confidenceThreshold === 'HIGH' && e.confidence >= 0.9) ||
        (confidenceThreshold === 'MEDIUM' && e.confidence >= 0.7);

      return endpointsMatch && relTypeMatch && confMatch;
    });

    return [
      ...visibleNodes.map((n) => ({
        data: {
          id: n.id,
          label: n.name.length > 20 ? n.name.slice(0, 19) + '…' : n.name,
          fullLabel: n.name,
          type: n.type.toUpperCase(),
          color: TYPE_COLORS[n.type.toUpperCase()] || '#8b949e',
          connections: n.connectionsCount,
        },
      })),
      ...visibleEdges.map((e) => ({
        data: {
          id: e.id,
          source: e.source,
          target: e.target,
          type: e.type,
          label: e.type,
          confidence: e.confidence,
        },
      })),
      ...(showModelLeadsOnGraph
        ? modelLeads
            .filter((l) => visibleNodeIds.has(l.sourceEntityId) && visibleNodeIds.has(l.targetEntityId))
            .map((l) => ({
              data: {
                id: `lead-edge-${l.id}`,
                source: l.sourceEntityId,
                target: l.targetEntityId,
                type: 'GAT_MODEL_LEAD',
                label: `GAT LEAD (${(l.score * 100).toFixed(0)}%)`,
                confidence: l.score,
                isModelLead: true,
              },
            }))
        : []),
    ];
  }, [nodes, edges, selectedTypes, selectedRelTypes, confidenceThreshold, showModelLeadsOnGraph, modelLeads, resolvedTheme]);

  // Initialize or re-render Cytoscape
  useEffect(() => {
    if (!cyRef.current || loading) return;

    if (cyInstance.current) {
      cyInstance.current.destroy();
      cyInstance.current = null;
    }

    const elements = getVisibleElements();

    const cy = cytoscape({
      container: cyRef.current,
      elements,
      style: [
        {
          selector: 'node',
          style: {
            'background-color': 'data(color)',
            'background-opacity': 0.18,
            'border-color': 'data(color)',
            'border-width': 2,
            'label': 'data(label)',
            'color': resolvedTheme === 'dark' ? '#e6edf3' : '#1f2328',
            'font-size': '11px',
            'font-family': 'Inter, system-ui, sans-serif',
            'font-weight': 600,
            'text-valign': 'bottom',
            'text-halign': 'center',
            'text-margin-y': 6,
            'width': 40,
            'height': 40,
          } as cytoscape.Css.Node,
        },
        {
          selector: 'node:selected',
          style: {
            'background-opacity': 0.45,
            'border-width': 3.5,
            'border-color': '#58a6ff',
            'width': 44,
            'height': 44,
          } as cytoscape.Css.Node,
        },
        {
          selector: 'node.highlighted',
          style: {
            'border-color': '#388bfd',
            'border-width': 4,
            'background-color': '#388bfd',
            'background-opacity': 0.5,
          } as cytoscape.Css.Node,
        },
        {
          selector: 'edge',
          style: {
            'width': 2,
            'line-color': resolvedTheme === 'dark' ? '#58a6ff' : '#0969da',
            'target-arrow-color': resolvedTheme === 'dark' ? '#58a6ff' : '#0969da',
            'target-arrow-shape': 'triangle',
            'curve-style': 'bezier',
            'opacity': 0.85,
            'label': 'data(type)',
            'font-size': '9px',
            'font-family': 'Inter, system-ui, sans-serif',
            'font-weight': 500,
            'text-wrap': 'ellipsis',
            'text-max-width': '90px',
            'color': resolvedTheme === 'dark' ? '#e6edf3' : '#1f2328',
            'text-background-color': resolvedTheme === 'dark' ? '#161b22' : '#ffffff',
            'text-background-opacity': 0.8,
            'text-background-padding': '2px',
            'text-rotation': 'autorotate',
            'text-margin-y': -8,
          } as cytoscape.Css.Edge,
        },
        {
          selector: 'edge:selected',
          style: {
            'width': 4,
            'opacity': 1,
            'line-color': resolvedTheme === 'dark' ? '#79c0ff' : '#0550ae',
            'target-arrow-color': resolvedTheme === 'dark' ? '#79c0ff' : '#0550ae',
            'color': resolvedTheme === 'dark' ? '#79c0ff' : '#0550ae',
          } as cytoscape.Css.Edge,
        },
        {
          selector: 'edge.highlighted',
          style: {
            'line-color': '#388bfd',
            'target-arrow-color': '#388bfd',
            'width': 3.5,
            'opacity': 1,
          } as cytoscape.Css.Edge,
        },
        {
          selector: 'edge[?isModelLead]',
          style: {
            'line-color': '#d29922',
            'line-style': 'dashed',
            'target-arrow-color': '#d29922',
            'target-arrow-shape': 'triangle',
            'curve-style': 'bezier',
            'width': 2.5,
            'opacity': 0.95,
            'color': '#d29922',
            'font-size': '9px',
            'text-rotation': 'autorotate',
            'text-background-color': resolvedTheme === 'dark' ? '#161b22' : '#ffffff',
            'text-background-opacity': 0.85,
            'text-background-padding': '2px',
          } as cytoscape.Css.Edge,
        },
      ],
      layout: {
        name: 'cose',
        animate: false,
        fit: true,
        padding: 70,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        nodeRepulsion: (() => 4500000) as any,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        idealEdgeLength: (() => 140) as any,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        edgeElasticity: (() => 0.45) as any,
        nodeDimensionsIncludeLabels: true,
      } as cytoscape.LayoutOptions,
      userZoomingEnabled: true,
      userPanningEnabled: true,
    });

    // Ensure clean initial viewport fit
    cy.fit(undefined, 60);

    // Node click
    cy.on('tap', 'node', (evt) => {
      const nodeId = evt.target.id();
      const found = nodes.find((n) => n.id === nodeId) || null;
      setSelectedNode(found);
      setSelectedEdge(null);
    });

    // Edge click
    cy.on('tap', 'edge', (evt) => {
      const edgeId = evt.target.id();
      const found = edges.find((e) => e.id === edgeId) || null;
      setSelectedEdge(found);
      setSelectedNode(null);
    });

    // Canvas background click
    cy.on('tap', (evt) => {
      if (evt.target === cy) {
        setSelectedNode(null);
        setSelectedEdge(null);
      }
    });

    cyInstance.current = cy;
    if (typeof window !== 'undefined') {
      (window as any).__cy = cy;
    }
    console.log(
      `Graph loaded:\nNodes: ${nodes.length}\nEdges: ${edges.length}\n\nCytoscape:\nNodes: ${cy.nodes().length}\nEdges: ${cy.edges().length}`
    );
    return () => {
      cy.destroy();
      cyInstance.current = null;
    };
  }, [getVisibleElements, loading, nodes, edges, resolvedTheme]);

  // Zoom / View handlers
  const handleZoomIn = () => cyInstance.current?.zoom(cyInstance.current.zoom() * 1.25);
  const handleZoomOut = () => cyInstance.current?.zoom(cyInstance.current.zoom() * 0.8);
  const handleFit = () => cyInstance.current?.fit(undefined, 35);
  const handleReset = () => {
    loadGraph();
  };

  // Expand Neighbors
  const handleExpandNeighbors = async (depth: number) => {
    if (!selectedNode) return;
    setExpandingNeighbors(true);

    try {
      const neighborhood = await graphService.getEntityNeighborhood(selectedNode.id, depth, effectiveCaseId);

      // Merge new nodes
      const existingNodeIds = new Set(nodes.map((n) => n.id));
      const newNodes = neighborhood.nodes.filter((n) => !existingNodeIds.has(n.id));

      // Merge new edges
      const existingEdgeIds = new Set(edges.map((e) => e.id));
      const newEdges = neighborhood.edges.filter((e) => !existingEdgeIds.has(e.id));

      if (newNodes.length > 0 || newEdges.length > 0) {
        setNodes((prev) => [...prev, ...newNodes]);
        setEdges((prev) => [...prev, ...newEdges]);
      }
    } catch (err) {
      console.error('Failed to expand entity neighborhood:', err);
    } finally {
      setExpandingNeighbors(false);
    }
  };

  // Backend Search
  const handleSearchChange = async (query: string) => {
    setSearchQuery(query);
    if (!query.trim()) {
      setSearchResults([]);
      setShowSearchDropdown(false);
      return;
    }

    setSearchLoading(true);
    setShowSearchDropdown(true);
    try {
      const results = await graphService.searchGraph(query, effectiveCaseId);
      setSearchResults(results);
    } catch (err) {
      console.error('Graph search error:', err);
    } finally {
      setSearchLoading(false);
    }
  };

  const handleSelectSearchResult = (result: GraphSearchResultDto) => {
    setSearchQuery(result.name);
    setShowSearchDropdown(false);

    if (cyInstance.current) {
      const cyNode = cyInstance.current.$(`#${result.id}`);
      if (cyNode.length > 0) {
        cyInstance.current.elements().removeClass('highlighted');
        cyNode.addClass('highlighted');
        cyInstance.current.animate({
          center: { eles: cyNode },
          zoom: 1.6,
          duration: 400,
        });

        const found = nodes.find((n) => n.id === result.id) || null;
        setSelectedNode(found);
        setSelectedEdge(null);
      }
    }
  };

  // Shortest Path Finder
  const handleFindShortestPath = async () => {
    if (!pathStartId || !pathEndId) return;
    setPathLoading(true);

    try {
      const res = await graphService.findShortestPath(pathStartId, pathEndId, pathMaxHops, effectiveCaseId);
      setPathResult(res);

      if (res.found && cyInstance.current) {
        cyInstance.current.elements().removeClass('highlighted');

        res.nodes.forEach((n) => {
          cyInstance.current?.$(`#${n.id}`).addClass('highlighted');
        });

        res.edges.forEach((e) => {
          cyInstance.current?.$(`#${e.id}`).addClass('highlighted');
        });

        cyInstance.current.fit(cyInstance.current.$('.highlighted'), 50);
      }
    } catch (err) {
      console.error('Shortest path search failed:', err);
    } finally {
      setPathLoading(false);
    }
  };

  const handleClearPath = () => {
    setPathResult(null);
    if (cyInstance.current) {
      cyInstance.current.elements().removeClass('highlighted');
      cyInstance.current.fit(undefined, 35);
    }
  };

  // Open Graph Statistics Modal
  const handleOpenStatistics = async () => {
    setShowStatsModal(true);
    setStatsLoading(true);
    try {
      const stats = await graphService.getGraphStatistics(effectiveCaseId);
      setStatistics(stats);
    } catch (err) {
      console.error('Failed to load graph statistics:', err);
    } finally {
      setStatsLoading(false);
    }
  };

  // Toggle Filters
  const toggleType = (t: string) => {
    setSelectedTypes((prev) => {
      const next = new Set(prev);
      if (next.has(t)) next.delete(t);
      else next.add(t);
      return next;
    });
  };

  const toggleRelType = (t: string) => {
    setSelectedRelTypes((prev) => {
      const next = new Set(prev);
      if (next.has(t)) next.delete(t);
      else next.add(t);
      return next;
    });
  };

  return (
    <div className="flex flex-col gap-4 pb-10" id="network-graph-page">
      {/* Top Header & Context */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-3 bg-[var(--color-surface)] border border-[var(--color-border)] p-4 rounded-xl">
        <div>
          <div className="flex items-center gap-2.5">
            <div className="p-1.5 rounded-lg bg-[#388bfd]/15 text-[#388bfd]">
              <GitBranch className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-lg font-bold text-[var(--color-text-primary)] tracking-tight">
                  {activeCaseNumber || 'CASE-2026-001'} Investigation Knowledge Graph
                </h1>
                <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-[#238636]/20 border border-[#238636]/40 text-[#3fb950] flex items-center gap-1">
                  <ShieldCheck className="w-3 h-3" /> VERIFIED ONLY
                </span>
              </div>
              <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">
                {activeCaseTitle || 'Operation Pune Hawala & Cyber Network'} · Verified entity associations with full forensic evidence provenance
              </p>
            </div>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={loadGraph}
            disabled={loading}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:border-[#388bfd]/40 text-xs transition-all cursor-pointer disabled:opacity-50"
            title="Refresh graph data"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} /> Refresh
          </button>

          <button
            onClick={() => setShowPathModal(true)}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border)] text-[var(--color-text-secondary)] hover:text-[#58a6ff] hover:border-[#58a6ff]/40 text-xs transition-all cursor-pointer"
          >
            <Route className="w-3.5 h-3.5 text-[#58a6ff]" /> Find Path
          </button>

          <button
            onClick={() => setShowModelLeadsOnGraph((prev) => !prev)}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg border text-xs transition-all cursor-pointer ${
              showModelLeadsOnGraph
                ? 'bg-[#d29922]/20 border-[#d29922] text-[#e3b341]'
                : 'bg-[var(--color-bg-canvas)] border-[var(--color-border)] text-[var(--color-text-secondary)] hover:text-[#d29922]'
            }`}
            title="Toggle GAT Model-Generated Leads overlay"
          >
            <Sparkles className="w-3.5 h-3.5 text-[#d29922]" />
            {showModelLeadsOnGraph ? 'Hide Model Leads' : 'Show Model Leads'}
          </button>

          <button
            onClick={() => navigate('/analytics')}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border)] text-[var(--color-text-secondary)] hover:text-[#3fb950] hover:border-[#3fb950]/40 text-xs transition-all cursor-pointer"
            title="Open Graph Analytics & GAT Dashboard"
          >
            <BarChart3 className="w-3.5 h-3.5 text-[#3fb950]" /> Analytics Dashboard
          </button>

          {/* Backend Search Input */}
          <div className="relative w-full sm:w-64">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-[#484f58]" />
            <input
              type="text"
              placeholder="Search suspect, vehicle, phone..."
              value={searchQuery}
              onChange={(e) => handleSearchChange(e.target.value)}
              className="w-full pl-8 pr-7 py-1.5 bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg text-xs text-[var(--color-text-primary)] placeholder-[#484f58] focus:outline-none focus:border-[#388bfd]"
            />
            {searchQuery && (
              <button
                onClick={() => {
                  setSearchQuery('');
                  setSearchResults([]);
                  setShowSearchDropdown(false);
                }}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-[#484f58] hover:text-[var(--color-text-secondary)]"
              >
                <X className="w-3 h-3" />
              </button>
            )}

            {/* Search Dropdown */}
            {showSearchDropdown && (
              <div className="absolute top-full left-0 right-0 mt-1 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg shadow-xl z-50 max-h-60 overflow-y-auto">
                {searchLoading ? (
                  <div className="p-3 text-center text-xs text-[var(--color-text-secondary)]">Searching knowledge graph...</div>
                ) : searchResults.length > 0 ? (
                  searchResults.map((r) => (
                    <button
                      key={r.id}
                      onClick={() => handleSelectSearchResult(r)}
                      className="w-full text-left px-3 py-2 border-b border-[var(--color-border-subtle)] last:border-0 hover:bg-[var(--color-bg-canvas)] transition-colors flex items-center justify-between"
                    >
                      <div>
                        <div className="text-xs font-semibold text-[var(--color-text-primary)]">{r.name}</div>
                        <div className="text-[10px] text-[var(--color-text-secondary)]">
                          {r.normalizedValue !== r.name && `${r.normalizedValue} · `}
                          {r.connectionsCount} connections
                        </div>
                      </div>
                      <span
                        className="text-[9px] font-bold px-1.5 py-0.5 rounded uppercase"
                        style={{
                          backgroundColor: `${TYPE_COLORS[r.type] || '#8b949e'}20`,
                          color: TYPE_COLORS[r.type] || '#8b949e',
                        }}
                      >
                        {r.type}
                      </span>
                    </button>
                  ))
                ) : (
                  <div className="p-3 text-center text-xs text-[var(--color-text-secondary)]">No matching entities found in this case</div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Main Graph Workbench */}
      <div className="flex gap-4" style={{ height: '650px' }}>
        {/* Left Filter & Legend Sidebar */}
        <div className="w-56 shrink-0 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl p-3.5 flex flex-col gap-4 overflow-y-auto">
          <div>
            <div className="flex items-center gap-1.5 text-[10px] font-bold text-[var(--color-text-secondary)] uppercase tracking-wider mb-2">
              <SlidersHorizontal className="w-3 h-3" /> Entity Categories
            </div>
            <div className="space-y-1">
              {CANONICAL_ENTITY_TYPES.map((t) => (
                <label key={t} className="flex items-center justify-between p-1 rounded hover:bg-[var(--color-bg-canvas)] cursor-pointer group">
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={selectedTypes.has(t)}
                      onChange={() => toggleType(t)}
                      className="w-3 h-3 rounded border border-[var(--color-border)] bg-[var(--color-bg-canvas)] accent-[#1f6feb]"
                    />
                    <span
                      className="text-xs font-medium group-hover:text-[var(--color-text-primary)] transition-colors"
                      style={{ color: selectedTypes.has(t) ? TYPE_COLORS[t] : '#484f58' }}
                    >
                      {t}
                    </span>
                  </div>
                  <span
                    className="w-2 h-2 rounded-full"
                    style={{ backgroundColor: TYPE_COLORS[t] || '#8b949e' }}
                  />
                </label>
              ))}
            </div>
          </div>

          <div>
            <div className="flex items-center gap-1.5 text-[10px] font-bold text-[var(--color-text-secondary)] uppercase tracking-wider mb-2">
              <Layers className="w-3 h-3" /> Relationship Types
            </div>
            <div className="space-y-1">
              {CANONICAL_RELATIONSHIP_TYPES.slice(0, 6).map((r) => (
                <label key={r} className="flex items-center gap-2 p-1 rounded hover:bg-[var(--color-bg-canvas)] cursor-pointer group">
                  <input
                    type="checkbox"
                    checked={selectedRelTypes.has(r)}
                    onChange={() => toggleRelType(r)}
                    className="w-3 h-3 rounded border border-[var(--color-border)] bg-[var(--color-bg-canvas)] accent-[#1f6feb]"
                  />
                  <span className="text-[11px] text-[var(--color-text-secondary)] group-hover:text-[var(--color-text-primary)] transition-colors truncate">
                    {r.replace(/_/g, ' ')}
                  </span>
                </label>
              ))}
            </div>
          </div>

          <div>
            <div className="text-[10px] font-bold text-[var(--color-text-secondary)] uppercase tracking-wider mb-2">Confidence Filter</div>
            <div className="grid grid-cols-3 gap-1 bg-[var(--color-bg-canvas)] p-1 rounded-lg border border-[var(--color-border)]">
              {(['ALL', 'MEDIUM', 'HIGH'] as const).map((conf) => (
                <button
                  key={conf}
                  onClick={() => setConfidenceThreshold(conf)}
                  className={`py-1 text-[10px] font-bold rounded cursor-pointer transition-all ${
                    confidenceThreshold === conf
                      ? 'bg-[#1f6feb] text-white'
                      : 'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
                  }`}
                >
                  {conf === 'ALL' ? 'All' : conf === 'MEDIUM' ? '≥70%' : '≥90%'}
                </button>
              ))}
            </div>
          </div>

          {/* Quick Stats Pill */}
          <div className="mt-auto pt-3 border-t border-[var(--color-border-subtle)] text-xs font-mono text-[var(--color-text-secondary)] space-y-1">
            <div className="flex justify-between">
              <span>Nodes:</span>
              <span className="text-[var(--color-text-primary)] font-bold">{nodes.length}</span>
            </div>
            <div className="flex justify-between">
              <span>Edges:</span>
              <span className="text-[var(--color-text-primary)] font-bold">{edges.length}</span>
            </div>
            {import.meta.env.DEV && (
              <div className="pt-1 mt-1 border-t border-[var(--color-border-subtle)] text-[10px] text-[var(--color-text-muted)] space-y-0.5">
                <div className="flex justify-between">
                  <span>Cy Nodes:</span>
                  <span className="text-[var(--color-accent)] font-bold">{cyInstance.current?.nodes().length ?? 0}</span>
                </div>
                <div className="flex justify-between">
                  <span>Cy Edges:</span>
                  <span className="text-[var(--color-accent)] font-bold">{cyInstance.current?.edges().length ?? 0}</span>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Center Cytoscape Canvas */}
        <div className="flex-1 bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-xl relative overflow-hidden">
          {loading && (
            <div className="absolute inset-0 flex items-center justify-center bg-[var(--color-bg-canvas)]/85 z-20">
              <div className="flex flex-col items-center gap-3">
                <div className="w-8 h-8 border-2 border-[var(--color-border)] border-t-[#388bfd] rounded-full animate-spin" />
                <p className="text-xs text-[var(--color-text-secondary)]">Loading case-scoped intelligence graph...</p>
              </div>
            </div>
          )}

          {/* Canvas Floating Tools */}
          <div className="absolute top-3 right-3 z-10 flex flex-col gap-1.5 bg-[var(--color-surface)] border border-[var(--color-border)] p-1 rounded-lg shadow-lg">
            <button
              onClick={handleZoomIn}
              className="w-7 h-7 flex items-center justify-center rounded text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] transition-colors"
              title="Zoom In"
            >
              <ZoomIn className="w-4 h-4" />
            </button>
            <button
              onClick={handleZoomOut}
              className="w-7 h-7 flex items-center justify-center rounded text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] transition-colors"
              title="Zoom Out"
            >
              <ZoomOut className="w-4 h-4" />
            </button>
            <button
              onClick={handleFit}
              className="w-7 h-7 flex items-center justify-center rounded text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] transition-colors"
              title="Fit Graph"
            >
              <Maximize2 className="w-4 h-4" />
            </button>
            <button
              onClick={handleReset}
              className="w-7 h-7 flex items-center justify-center rounded text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] transition-colors"
              title="Reset Layout"
            >
              <RefreshCw className="w-3.5 h-3.5" />
            </button>
          </div>

          {/* Path Trace Badge */}
          {pathResult?.found && (
            <div className="absolute top-3 left-3 z-10 bg-[var(--color-surface)] border border-[#58a6ff]/40 px-3 py-2 rounded-lg shadow-lg flex items-center gap-3">
              <div>
                <div className="text-xs font-bold text-[#58a6ff]">Shortest Path Active</div>
                <div className="text-[10px] text-[var(--color-text-secondary)]">
                  {pathResult.hopsCount} hops between {pathResult.nodes[0]?.name} and {pathResult.nodes[pathResult.nodes.length - 1]?.name}
                </div>
              </div>
              <button
                onClick={handleClearPath}
                className="px-2 py-1 text-[10px] font-bold rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
              >
                Clear
              </button>
            </div>
          )}

          {/* Cytoscape Container */}
          <div ref={cyRef} className="w-full h-full cy-container" />
        </div>

        {/* Right Details Panel: Entity or Relationship Selection */}
        <div className="w-72 shrink-0 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl flex flex-col overflow-hidden">
          {selectedNode ? (
            <div className="flex flex-col h-full overflow-y-auto">
              {/* Entity Header */}
              <div className="p-3.5 border-b border-[var(--color-border-subtle)] bg-[var(--color-bg-canvas)]">
                <div className="flex items-center justify-between gap-2">
                  <span
                    className="text-[9px] font-bold px-2 py-0.5 rounded uppercase"
                    style={{
                      backgroundColor: `${TYPE_COLORS[selectedNode.type] || '#8b949e'}20`,
                      color: TYPE_COLORS[selectedNode.type] || '#8b949e',
                    }}
                  >
                    {selectedNode.type}
                  </span>
                  <span className="flex items-center gap-1 text-[10px] font-bold text-[#3fb950]">
                    <CheckCircle2 className="w-3 h-3" /> VERIFIED
                  </span>
                </div>
                <h3 className="text-sm font-bold text-[var(--color-text-primary)] mt-2 leading-tight">{selectedNode.name}</h3>
                <p className="text-[10px] font-mono text-[var(--color-text-secondary)] mt-0.5">ID: {selectedNode.id}</p>
              </div>

              {/* Entity Metadata */}
              <div className="p-3.5 space-y-3 flex-1">
                <div className="grid grid-cols-2 gap-2 bg-[var(--color-bg-canvas)] p-2.5 rounded-lg border border-[var(--color-border)] text-center">
                  <div>
                    <div className="text-[10px] text-[var(--color-text-secondary)]">Connections</div>
                    <div className="text-base font-bold text-[var(--color-text-primary)]">{selectedNode.connectionsCount}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-[var(--color-text-secondary)]">Evidence</div>
                    <div className="text-base font-bold text-[var(--color-text-primary)]">{selectedNode.evidenceCount}</div>
                  </div>
                </div>

                {Boolean(selectedNode.properties['normalized_value']) && (
                  <div>
                    <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] mb-0.5">Normalized Value</div>
                    <div className="text-xs font-mono text-[var(--color-text-primary)] bg-[var(--color-bg-canvas)] px-2 py-1 rounded border border-[var(--color-border-subtle)]">
                      {String(selectedNode.properties['normalized_value'])}
                    </div>
                  </div>
                )}

                {Boolean(selectedNode.properties['location']) && (
                  <div>
                    <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] mb-0.5">Location</div>
                    <div className="text-xs text-[var(--color-text-primary)]">{String(selectedNode.properties['location'])}</div>
                  </div>
                )}

                {/* Phase 5: Graph Centrality & GAT ML Signals */}
                {nodeAnalytics?.metrics && (
                  <div className="pt-2 border-t border-[var(--color-border-subtle)] space-y-2">
                    <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] flex items-center justify-between">
                      <span className="flex items-center gap-1">
                        <BarChart3 className="w-3 h-3 text-[#58a6ff]" /> Centrality & ML Leads
                      </span>
                      <span className="text-[9px] font-mono px-1.5 py-0.2 rounded bg-[var(--color-surface-subtle)] text-[#e3b341]">
                        {nodeAnalytics.metrics.analyticalIndicator}
                      </span>
                    </div>

                    <div className="grid grid-cols-2 gap-1.5 bg-[var(--color-bg-canvas)] p-2 rounded border border-[var(--color-border)] text-[11px]">
                      <div>
                        <span className="text-[var(--color-text-secondary)] text-[9px] block">Betweenness:</span>
                        <span className="font-mono text-[#58a6ff] font-bold">
                          {nodeAnalytics.metrics.betweennessCentrality.toFixed(3)}
                        </span>
                      </div>
                      <div>
                        <span className="text-[var(--color-text-secondary)] text-[9px] block">Degree Centrality:</span>
                        <span className="font-mono text-[var(--color-text-primary)] font-bold">
                          {nodeAnalytics.metrics.degree}
                        </span>
                      </div>
                      <div>
                        <span className="text-[var(--color-text-secondary)] text-[9px] block">Closeness:</span>
                        <span className="font-mono text-[var(--color-text-secondary)]">
                          {nodeAnalytics.metrics.closenessCentrality.toFixed(3)}
                        </span>
                      </div>
                      <div>
                        <span className="text-[var(--color-text-secondary)] text-[9px] block">PageRank:</span>
                        <span className="font-mono text-[var(--color-text-secondary)]">
                          {nodeAnalytics.metrics.pageRank.toFixed(4)}
                        </span>
                      </div>
                    </div>

                    {nodeAnalytics.adjacentLeads.length > 0 && (
                      <div className="p-2 rounded bg-[#d29922]/10 border border-[#d29922]/30 text-[10px] text-[#e3b341] flex items-center justify-between">
                        <span>{nodeAnalytics.adjacentLeads.length} GAT link leads available</span>
                        <button
                          onClick={() => navigate('/analytics')}
                          className="text-[#58a6ff] underline font-medium hover:text-[#79c0ff]"
                        >
                          Review Leads
                        </button>
                      </div>
                    )}
                  </div>
                )}

                {/* Multi-hop Expand Neighbors */}
                <div className="pt-2 border-t border-[var(--color-border-subtle)]">
                  <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] mb-2 flex items-center gap-1">
                    <GitBranch className="w-3 h-3 text-[#388bfd]" /> Expand Neighborhood
                  </div>
                  <div className="grid grid-cols-3 gap-1.5">
                    {[1, 2, 3].map((depth) => (
                      <button
                        key={depth}
                        onClick={() => handleExpandNeighbors(depth)}
                        disabled={expandingNeighbors}
                        className="px-2 py-1.5 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border)] hover:border-[#388bfd] text-[11px] font-semibold text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-all disabled:opacity-50 cursor-pointer"
                      >
                        +{depth} Hop
                      </button>
                    ))}
                  </div>
                </div>

                {/* Geospatial Map Navigation */}
                <div className="pt-2 border-t border-[var(--color-border-subtle)]">
                  <button
                    onClick={() => {
                      if (selectedNode.type === 'LOCATION') {
                        navigate(`/map?locationId=${encodeURIComponent(selectedNode.id)}&caseId=${encodeURIComponent(effectiveCaseId)}`);
                      } else {
                        navigate(`/map?entityId=${encodeURIComponent(selectedNode.id)}&caseId=${encodeURIComponent(effectiveCaseId)}`);
                      }
                    }}
                    className="w-full flex items-center justify-center gap-1.5 py-2 px-3 rounded-lg bg-[#10b981]/15 hover:bg-[#10b981]/25 border border-[#10b981]/40 text-[#10b981] text-xs font-semibold transition cursor-pointer"
                  >
                    <MapPin className="w-3.5 h-3.5" />
                    <span>{selectedNode.type === 'LOCATION' ? 'View Location on Map' : 'Trace Trajectory on Map'}</span>
                  </button>
                </div>

                {/* Connected Relationships List */}
                <div className="pt-2 border-t border-[var(--color-border-subtle)]">
                  <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] mb-2">Connected Relationships</div>
                  <div className="space-y-1.5 max-h-48 overflow-y-auto">
                    {edges
                      .filter((e) => e.source === selectedNode.id || e.target === selectedNode.id)
                      .map((e) => {
                        const otherId = e.source === selectedNode.id ? e.target : e.source;
                        const otherNode = nodes.find((n) => n.id === otherId);
                        return (
                          <button
                            key={e.id}
                            onClick={() => {
                              setSelectedEdge(e);
                              setSelectedNode(null);
                            }}
                            className="w-full text-left p-2 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border-subtle)] hover:border-[#388bfd]/50 transition-colors flex items-center justify-between group"
                          >
                            <div className="truncate">
                              <div className="text-[10px] font-bold text-[#58a6ff]">{e.type.replace(/_/g, ' ')}</div>
                              <div className="text-[11px] text-[var(--color-text-primary)] truncate">{otherNode?.name || otherId}</div>
                            </div>
                            <ChevronRight className="w-3.5 h-3.5 text-[#484f58] group-hover:text-[#58a6ff]" />
                          </button>
                        );
                      })}
                  </div>
                </div>

                {/* Phase 6: Entity Activity Timeline */}
                <div className="pt-2 border-t border-[var(--color-border-subtle)] space-y-2">
                  <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] flex items-center justify-between">
                    <span className="flex items-center gap-1">
                      <Clock className="w-3 h-3 text-[#d29922]" /> Activity Timeline ({nodeTimelineEvents.length})
                    </span>
                    <button
                      onClick={() => navigate(`/timeline?entityId=${selectedNode.id}`)}
                      className="text-[10px] text-[#58a6ff] hover:underline flex items-center gap-0.5"
                    >
                      Full Timeline <ExternalLink className="w-2.5 h-2.5" />
                    </button>
                  </div>

                  {loadingNodeTimeline ? (
                    <div className="text-center py-2 text-xs text-[var(--color-text-secondary)]">Loading temporal events...</div>
                  ) : nodeTimelineEvents.length === 0 ? (
                    <div className="text-[11px] text-[var(--color-text-secondary)] bg-[var(--color-bg-canvas)] p-2 rounded border border-[var(--color-border-subtle)]">
                      No recorded chronological events for this entity.
                    </div>
                  ) : (
                    <div className="space-y-1.5 max-h-48 overflow-y-auto">
                      {nodeTimelineEvents.slice(0, 4).map((ev) => (
                        <div key={ev.id} className="p-2 bg-[var(--color-bg-canvas)] border border-[var(--color-border-subtle)] rounded text-xs space-y-1">
                          <div className="flex items-center justify-between">
                            <span className="text-[10px] font-mono text-[#58a6ff]">
                              {ev.startTimeUtc ? new Date(ev.startTimeUtc).toLocaleDateString('en-IN', { month: 'short', day: 'numeric', year: 'numeric' }) : 'Date Unknown'}
                            </span>
                            <span className="text-[9px] font-semibold px-1.5 py-0.2 bg-[#d29922]/20 text-[#e3b341] rounded border border-[#d29922]/30">
                              {ev.eventType.replace(/_/g, ' ')}
                            </span>
                          </div>
                          <p className="text-[11px] text-[var(--color-text-primary)] line-clamp-2">{ev.description}</p>
                          {ev.location && (
                            <div className="text-[10px] text-[var(--color-text-secondary)] flex items-center gap-1">
                              <span>📍 {ev.location}</span>
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                {/* Cross-Case Matches Section */}
                {nodeCrossCaseMatches.length > 0 && (
                  <div className="pt-2 border-t border-[var(--color-border-subtle)]">
                    <div className="text-[10px] uppercase font-bold text-[#58a6ff] mb-2 flex items-center justify-between">
                      <span className="flex items-center gap-1">
                        <GitMerge className="w-3 h-3" /> Cross-Case Matches ({nodeCrossCaseMatches.length})
                      </span>
                      <button
                        onClick={() => navigate('/entity-resolution')}
                        className="text-[10px] text-[#58a6ff] hover:underline"
                      >
                        View All
                      </button>
                    </div>
                    <div className="space-y-1.5">
                      {nodeCrossCaseMatches.map((m) => {
                        const otherCase = m.sourceEntityId === selectedNode.id ? m.targetCaseNumber : m.sourceCaseNumber;
                        const otherName = m.sourceEntityId === selectedNode.id ? m.targetEntity?.canonicalName : m.sourceEntity?.canonicalName;
                        return (
                          <div
                            key={m.id}
                            className="p-2 bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded text-xs space-y-1"
                          >
                            <div className="flex items-center justify-between">
                              <span className="text-[10px] font-mono text-[#58a6ff]">{otherCase}</span>
                              <span className="text-[10px] font-mono px-1.5 py-0.2 bg-emerald-500/20 text-emerald-400 rounded">
                                {Math.round(m.matchScore * 100)}% Match
                              </span>
                            </div>
                            <div className="text-[11px] font-semibold text-[var(--color-text-primary)] truncate">{otherName}</div>
                            <button
                              onClick={() => navigate('/entity-resolution')}
                              className="w-full text-center text-[10px] py-1 bg-[#1f6feb]/20 hover:bg-[#1f6feb]/30 text-[#58a6ff] rounded border border-[#1f6feb]/30 transition-colors"
                            >
                              Review Potential Match
                            </button>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}
              </div>
            </div>
          ) : selectedEdge ? (
            <div className="flex flex-col h-full overflow-y-auto">
              {/* Relationship Header */}
              <div className="p-3.5 border-b border-[var(--color-border-subtle)] bg-[var(--color-bg-canvas)]">
                <span className="text-[9px] font-bold px-2 py-0.5 rounded uppercase bg-[#388bfd]/15 text-[#388bfd] border border-[#388bfd]/30">
                  RELATIONSHIP
                </span>
                <h3 className="text-sm font-bold text-[var(--color-text-primary)] mt-2">{selectedEdge.type.replace(/_/g, ' ')}</h3>
                <div className="flex items-center gap-1.5 mt-1 text-[11px] text-[#3fb950] font-semibold">
                  <CheckCircle2 className="w-3 h-3" /> Confidence: {Math.round(selectedEdge.confidence * 100)}%
                </div>
              </div>

              {/* Endpoints */}
              <div className="p-3.5 space-y-3 flex-1">
                <div className="bg-[var(--color-bg-canvas)] p-2.5 rounded-lg border border-[var(--color-border)] space-y-2">
                  <div>
                    <div className="text-[9px] uppercase font-bold text-[var(--color-text-secondary)]">Source Entity</div>
                    <div className="text-xs font-semibold text-[var(--color-text-primary)]">
                      {edgeDetails?.sourceEntityName || selectedEdge.source}
                    </div>
                  </div>
                  <div className="flex justify-center text-[#484f58]">
                    <ArrowRight className="w-3.5 h-3.5" />
                  </div>
                  <div>
                    <div className="text-[9px] uppercase font-bold text-[var(--color-text-secondary)]">Target Entity</div>
                    <div className="text-xs font-semibold text-[var(--color-text-primary)]">
                      {edgeDetails?.targetEntityName || selectedEdge.target}
                    </div>
                  </div>
                </div>

                {/* Provenance Details */}
                <div className="space-y-2.5">
                  <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] flex items-center gap-1">
                    <FileText className="w-3 h-3 text-[#58a6ff]" /> Evidence Provenance
                  </div>

                  {edgeDetails?.supportingEvidence && edgeDetails.supportingEvidence.length > 0 ? (
                    <div className="space-y-2">
                      {edgeDetails.supportingEvidence.map((ev, idx) => (
                        <div key={idx} className="p-2.5 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border-subtle)] space-y-1.5">
                          <div className="flex items-center justify-between">
                            <span className="text-xs font-semibold text-[var(--color-text-primary)] truncate">{ev.fileName}</span>
                            <span className="text-[9px] font-mono text-[var(--color-text-secondary)]">{ev.evidenceId.slice(0, 8)}</span>
                          </div>
                          {ev.sourceLocation && (
                            <div className="text-[11px] text-[var(--color-text-secondary)] italic leading-relaxed">
                              &ldquo;{ev.sourceLocation}&rdquo;
                            </div>
                          )}
                          <div className="flex items-center justify-between pt-1 border-t border-[var(--color-border-subtle)] text-[10px] text-[#484f58]">
                            <span>By: {ev.verifiedBy}</span>
                            <span>{Math.round(ev.confidence * 100)}%</span>
                          </div>
                          <button
                            onClick={() => navigate(`/evidence/${ev.evidenceId}`)}
                            className="w-full mt-1 px-2.5 py-1.5 rounded bg-[#1f6feb]/15 border border-[#1f6feb]/30 text-[#58a6ff] hover:bg-[#1f6feb]/25 text-xs font-bold transition-all flex items-center justify-center gap-1.5 cursor-pointer"
                          >
                            <ExternalLink className="w-3 h-3" /> [OPEN EVIDENCE]
                          </button>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="p-3 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border-subtle)] text-center space-y-2">
                      <div className="text-xs text-[var(--color-text-secondary)]">
                        Evidence ID: {selectedEdge.sourceEvidenceId || 'Source Evidence'}
                      </div>
                      {selectedEdge.sourceLocation && (
                        <div className="text-[11px] text-[var(--color-text-secondary)] italic">
                          &ldquo;{selectedEdge.sourceLocation}&rdquo;
                        </div>
                      )}
                      {selectedEdge.sourceEvidenceId && (
                        <button
                          onClick={() => navigate(`/evidence/${selectedEdge.sourceEvidenceId}`)}
                          className="w-full px-2.5 py-1.5 rounded bg-[#1f6feb]/15 border border-[#1f6feb]/30 text-[#58a6ff] hover:bg-[#1f6feb]/25 text-xs font-bold transition-all flex items-center justify-center gap-1.5 cursor-pointer"
                        >
                          <ExternalLink className="w-3 h-3" /> [OPEN EVIDENCE]
                        </button>
                      )}
                    </div>
                  )}
                </div>

                {/* Phase 6: Temporal Interactions for Relationship */}
                <div className="space-y-2 pt-2 border-t border-[var(--color-border-subtle)]">
                  <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] flex items-center justify-between">
                    <span className="flex items-center gap-1">
                      <Clock className="w-3 h-3 text-[#d29922]" /> Temporal Interactions ({edgeTimelineEvents.length})
                    </span>
                    <button
                      onClick={() => navigate('/timeline')}
                      className="text-[10px] text-[#58a6ff] hover:underline flex items-center gap-0.5"
                    >
                      Timeline <ExternalLink className="w-2.5 h-2.5" />
                    </button>
                  </div>

                  {loadingEdgeTimeline ? (
                    <div className="text-center py-2 text-xs text-[var(--color-text-secondary)]">Loading events...</div>
                  ) : edgeTimelineEvents.length === 0 ? (
                    <div className="text-[11px] text-[var(--color-text-secondary)] bg-[var(--color-bg-canvas)] p-2.5 rounded border border-[var(--color-border-subtle)]">
                      No timestamped interaction events indexed between these endpoints.
                    </div>
                  ) : (
                    <div className="space-y-1.5 max-h-40 overflow-y-auto">
                      {edgeTimelineEvents.slice(0, 3).map((ev) => (
                        <div key={ev.id} className="p-2 bg-[var(--color-bg-canvas)] border border-[var(--color-border-subtle)] rounded text-xs space-y-1">
                          <div className="flex items-center justify-between">
                            <span className="text-[10px] font-mono text-[#58a6ff]">
                              {ev.startTimeUtc ? new Date(ev.startTimeUtc).toLocaleString('en-IN', { dateStyle: 'short', timeStyle: 'short' }) : 'Date Unknown'}
                            </span>
                            <span className="text-[9px] font-semibold px-1 py-0.2 bg-[#388bfd]/15 text-[#58a6ff] rounded">
                              {ev.eventType}
                            </span>
                          </div>
                          <p className="text-[11px] text-[var(--color-text-primary)] line-clamp-2">{ev.description}</p>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>
          ) : (
            <div className="flex-1 flex flex-col items-center justify-center p-6 text-center text-[var(--color-text-secondary)]">
              <div className="w-12 h-12 rounded-full bg-[var(--color-surface-subtle)] flex items-center justify-center mb-3">
                <GitBranch className="w-6 h-6 text-[#484f58]" />
              </div>
              <p className="text-xs font-semibold text-[var(--color-text-primary)]">Knowledge Graph Navigator</p>
              <p className="text-[11px] mt-1 text-[var(--color-text-secondary)] leading-relaxed">
                Click any node to explore connections &amp; multi-hop neighbors, or click an edge to inspect source evidence provenance.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Shortest Path Finder Modal */}
      {showPathModal && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
          <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl w-full max-w-lg shadow-2xl p-5 space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <Route className="w-5 h-5 text-[#58a6ff]" />
                <h3 className="text-sm font-bold text-[var(--color-text-primary)]">Find Shortest Investigative Path</h3>
              </div>
              <button onClick={() => setShowPathModal(false)} className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="space-y-3">
              <div>
                <label className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] block mb-1">Origin Entity (A)</label>
                <select
                  value={pathStartId}
                  onChange={(e) => setPathStartId(e.target.value)}
                  className="w-full px-3 py-2 bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg text-xs text-[var(--color-text-primary)]"
                >
                  <option value="">Select origin entity...</option>
                  {nodes.map((n) => (
                    <option key={n.id} value={n.id}>
                      {n.name} ({n.type})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] block mb-1">Destination Entity (B)</label>
                <select
                  value={pathEndId}
                  onChange={(e) => setPathEndId(e.target.value)}
                  className="w-full px-3 py-2 bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg text-xs text-[var(--color-text-primary)]"
                >
                  <option value="">Select destination entity...</option>
                  {nodes.map((n) => (
                    <option key={n.id} value={n.id}>
                      {n.name} ({n.type})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)] block mb-1">Maximum Hops (1–6)</label>
                <input
                  type="number"
                  min="1"
                  max="6"
                  value={pathMaxHops}
                  onChange={(e) => setPathMaxHops(parseInt(e.target.value) || 4)}
                  className="w-full px-3 py-2 bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg text-xs text-[var(--color-text-primary)]"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 pt-3 border-t border-[var(--color-border-subtle)]">
              <button
                onClick={() => setShowPathModal(false)}
                className="px-3 py-1.5 rounded-lg border border-[var(--color-border)] text-xs font-semibold text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  handleFindShortestPath();
                  setShowPathModal(false);
                }}
                disabled={!pathStartId || !pathEndId || pathLoading}
                className="px-4 py-1.5 rounded-lg bg-[#1f6feb] hover:bg-[#388bfd] text-xs font-bold text-white transition-all disabled:opacity-50"
              >
                {pathLoading ? 'Tracing...' : 'Find Connection Path'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Graph Analytics & Statistics Modal */}
      {showStatsModal && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
          <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl w-full max-w-2xl shadow-2xl p-5 space-y-4 max-h-[85vh] flex flex-col">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <BarChart3 className="w-5 h-5 text-[#3fb950]" />
                <div>
                  <h3 className="text-sm font-bold text-[var(--color-text-primary)]">Case Network Graph Analytics</h3>
                  <p className="text-[10px] text-[var(--color-text-secondary)]">Objective structural indicators calculated from backend graph store</p>
                </div>
              </div>
              <button onClick={() => setShowStatsModal(false)} className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]">
                <X className="w-4 h-4" />
              </button>
            </div>

            {statsLoading ? (
              <div className="p-12 text-center text-xs text-[var(--color-text-secondary)]">Calculating network metrics...</div>
            ) : statistics ? (
              <div className="space-y-4 overflow-y-auto pr-1">
                {/* Metric Cards */}
                <div className="grid grid-cols-3 gap-3">
                  <div className="p-3 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border)]">
                    <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)]">Total Nodes</div>
                    <div className="text-xl font-bold text-[var(--color-text-primary)] mt-0.5">{statistics.totalNodes}</div>
                  </div>
                  <div className="p-3 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border)]">
                    <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)]">Total Edges</div>
                    <div className="text-xl font-bold text-[var(--color-text-primary)] mt-0.5">{statistics.totalEdges}</div>
                  </div>
                  <div className="p-3 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border)]">
                    <div className="text-[10px] uppercase font-bold text-[var(--color-text-secondary)]">Network Clusters</div>
                    <div className="text-xl font-bold text-[var(--color-text-primary)] mt-0.5">{statistics.connectedComponents.length}</div>
                  </div>
                </div>

                {/* Degree Centrality Table */}
                <div>
                  <div className="text-xs font-bold text-[var(--color-text-primary)] mb-2 flex items-center gap-1.5">
                    <Info className="w-3.5 h-3.5 text-[#58a6ff]" /> Degree Centrality Analysis
                  </div>
                  <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
                    <table className="w-full text-left text-xs">
                      <thead className="bg-[var(--color-bg-canvas)] text-[var(--color-text-secondary)] border-b border-[var(--color-border)] text-[10px] uppercase">
                        <tr>
                          <th className="p-2.5">Entity</th>
                          <th className="p-2.5">Category</th>
                          <th className="p-2.5">Degree</th>
                          <th className="p-2.5">Analytical Indicator</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-[var(--color-border-subtle)]">
                        {statistics.centralityRankings.map((c) => (
                          <tr key={c.entityId} className="hover:bg-[var(--color-bg-canvas)]">
                            <td className="p-2.5 font-semibold text-[var(--color-text-primary)]">{c.entityName}</td>
                            <td className="p-2.5">
                              <span
                                className="text-[9px] font-bold px-1.5 py-0.5 rounded uppercase"
                                style={{
                                  backgroundColor: `${TYPE_COLORS[c.entityType] || '#8b949e'}20`,
                                  color: TYPE_COLORS[c.entityType] || '#8b949e',
                                }}
                              >
                                {c.entityType}
                              </span>
                            </td>
                            <td className="p-2.5 font-mono text-[var(--color-text-primary)]">{c.degree}</td>
                            <td className="p-2.5 text-[var(--color-text-secondary)]">{c.analyticalIndicator}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>

                {/* Connected Components / Clusters */}
                <div>
                  <div className="text-xs font-bold text-[var(--color-text-primary)] mb-2">Connected Association Clusters</div>
                  <div className="grid grid-cols-2 gap-2">
                    {statistics.connectedComponents.map((comp) => (
                      <div key={comp.clusterId} className="p-3 rounded-lg bg-[var(--color-bg-canvas)] border border-[var(--color-border-subtle)] space-y-1">
                        <div className="text-xs font-bold text-[#58a6ff]">{comp.clusterLabel}</div>
                        <div className="text-[11px] text-[var(--color-text-secondary)]">
                          {comp.entityCount} Entities · {comp.relationshipCount} Relationships
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            ) : null}

            <div className="pt-3 border-t border-[var(--color-border-subtle)] flex justify-end">
              <button
                onClick={() => setShowStatsModal(false)}
                className="px-4 py-1.5 rounded-lg bg-[var(--color-surface-subtle)] hover:bg-[#30363d] text-xs font-bold text-[var(--color-text-primary)]"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
