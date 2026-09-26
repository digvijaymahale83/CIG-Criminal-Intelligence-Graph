import React, { useEffect, useRef, useState, useCallback } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import {
  Map as MapIcon,
  MapPin,
  Navigation,
  Compass,
  Layers,
  Search,
  Play,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  ShieldCheck,
  RefreshCw,
  X,
  ChevronRight,
  Clock,
  FileText,
  Activity,
  ExternalLink,
  SlidersHorizontal,
  Crosshair,
  Route
} from 'lucide-react';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { Card } from '../../components/common/Card';
import { StatusBadge } from '../../components/common/StatusBadge';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';
import { useTheme } from '../../hooks/useTheme';
import {
  geospatialService,
  CaseMapDto,
  LocationDto,
  SpatialClusterDto,
  SpatialSignalDto,
  LocationActivityDto,
  TravelSequenceDto,
  SpatialProximityResultDto
} from '../../services/geospatial/geospatial.service';

export const GeospatialMapPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { activeCaseId, activeCaseNumber, activeCaseTitle } = useActiveInvestigation();
  const effectiveCaseId = searchParams.get('caseId') || activeCaseId || 'case-2026-001';
  const { resolvedTheme } = useTheme();
  const tileLayerRef = useRef<L.TileLayer | null>(null);

  // Navigation / Query pre-selection
  const queryLocationId = searchParams.get('locationId');
  const queryEntityId = searchParams.get('entityId');

  // UI Modes
  const [activeTab, setActiveTab] = useState<'MAP' | 'TRAJECTORY' | 'SIGNALS' | 'PROXIMITY'>('MAP');

  // State
  const [mapData, setMapData] = useState<CaseMapDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Selected Location for Detail Drawer
  const [selectedLocation, setSelectedLocation] = useState<LocationDto | null>(null);
  const [locationActivity, setLocationActivity] = useState<LocationActivityDto | null>(null);
  const [loadingActivity, setLoadingActivity] = useState<boolean>(false);

  // Trajectory Analysis
  const [selectedEntityId, setSelectedEntityId] = useState<string>(queryEntityId || '');
  const [travelSequence, setTravelSequence] = useState<TravelSequenceDto | null>(null);
  const [loadingTrajectory, setLoadingTrajectory] = useState<boolean>(false);

  // Spatial Signals
  const [signals, setSignals] = useState<SpatialSignalDto[]>([]);
  const [signalStatusFilter, setSignalStatusFilter] = useState<string>('ALL');
  const [reviewingSignal, setReviewingSignal] = useState<SpatialSignalDto | null>(null);
  const [reviewNotes, setReviewNotes] = useState<string>('');
  const [submittingReview, setSubmittingReview] = useState<boolean>(false);

  // Proximity Tool
  const [proximityRadiusKm, setProximityRadiusKm] = useState<number>(25);
  const [proximityCenter, setProximityCenter] = useState<[number, number] | null>(null);
  const [proximityResults, setProximityResults] = useState<SpatialProximityResultDto[]>([]);
  const [searchingProximity, setSearchingProximity] = useState<boolean>(false);

  // Map Instance
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markersLayerRef = useRef<L.LayerGroup | null>(null);
  const clustersLayerRef = useRef<L.LayerGroup | null>(null);
  const trajectoryLayerRef = useRef<L.LayerGroup | null>(null);
  const proximityLayerRef = useRef<L.LayerGroup | null>(null);

  // Fetch Map Data
  const loadMapData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await geospatialService.getCaseMapData(effectiveCaseId);
      setMapData(data);
      setSignals(data.activeSignals || []);

      // If query specified a location, select it
      if (queryLocationId && data.locations) {
        const target = data.locations.find(l => l.id === queryLocationId);
        if (target) {
          handleSelectLocation(target);
        }
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to load case map data';
      setError(msg);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [effectiveCaseId, queryLocationId]);

  useEffect(() => {
    loadMapData();
  }, [loadMapData]);

  // Handle Location Click & Fetch Detailed Activity
  const handleSelectLocation = async (loc: LocationDto) => {
    setSelectedLocation(loc);
    setLoadingActivity(true);
    try {
      const activity = await geospatialService.getLocationActivity(loc.id, effectiveCaseId);
      setLocationActivity(activity);
    } catch {
      setLocationActivity(null);
    } finally {
      setLoadingActivity(false);
    }

    // Pan map to location
    if (mapInstanceRef.current) {
      mapInstanceRef.current.setView([loc.latitude, loc.longitude], 13, { animate: true });
    }
  };

  // Load Trajectory for an Entity
  const loadTrajectory = async (entityId: string) => {
    if (!entityId) return;
    setLoadingTrajectory(true);
    try {
      const seq = await geospatialService.getEntityTravelSequence(entityId, effectiveCaseId);
      setTravelSequence(seq);
      setActiveTab('TRAJECTORY');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to load travel sequence';
      setError(msg);
    } finally {
      setLoadingTrajectory(false);
    }
  };

  useEffect(() => {
    if (queryEntityId) {
      setSelectedEntityId(queryEntityId);
      loadTrajectory(queryEntityId);
    }
  }, [queryEntityId]);

  // Run Proximity Search
  const runProximitySearch = async (lat: number, lon: number, radius: number) => {
    setSearchingProximity(true);
    setProximityCenter([lat, lon]);
    try {
      const matches = await geospatialService.searchProximity(effectiveCaseId, lat, lon, radius);
      setProximityResults(matches);
      setActiveTab('PROXIMITY');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Proximity query failed';
      setError(msg);
    } finally {
      setSearchingProximity(false);
    }
  };

  // Run Spatial Analysis
  const handleRunAnalysis = async () => {
    setRefreshing(true);
    try {
      await geospatialService.runSpatialAnalysis(effectiveCaseId, {
        clusterRadiusKm: 25.0,
        velocityWarningThresholdKmh: 900.0,
        includeCrossCase: false
      });
      await loadMapData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Analysis failed';
      setError(msg);
    } finally {
      setRefreshing(false);
    }
  };

  // Review Spatial Signal
  const handleReviewSignal = async (status: 'CONFIRMED' | 'DISMISSED') => {
    if (!reviewingSignal) return;
    setSubmittingReview(true);
    try {
      await geospatialService.reviewSpatialSignal(effectiveCaseId, reviewingSignal.id, {
        status,
        reviewNotes: reviewNotes || undefined
      });
      setReviewingSignal(null);
      setReviewNotes('');
      await loadMapData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to review signal';
      setError(msg);
    } finally {
      setSubmittingReview(false);
    }
  };

  // Initialize Leaflet Map
  useEffect(() => {
    if (!mapContainerRef.current || mapInstanceRef.current) return;

    // Create Map centered at Maharashtra / Central India
    const map = L.map(mapContainerRef.current, {
      center: [18.75, 73.5],
      zoom: 8,
      zoomControl: false,
      attributionControl: false
    });

    // Dynamic Basemap CartoDB (follows resolvedTheme)
    const tileUrl = resolvedTheme === 'dark'
      ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
      : 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';

    tileLayerRef.current = L.tileLayer(tileUrl, {
      attribution: '© CartoDB',
      subdomains: 'abcd',
      maxZoom: 19
    }).addTo(map);

    L.control.zoom({ position: 'topleft' }).addTo(map);

    // Create Layer Groups
    clustersLayerRef.current = L.layerGroup().addTo(map);
    markersLayerRef.current = L.layerGroup().addTo(map);
    trajectoryLayerRef.current = L.layerGroup().addTo(map);
    proximityLayerRef.current = L.layerGroup().addTo(map);

    mapInstanceRef.current = map;

    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, []);

  // Render Markers and Clusters whenever mapData updates
  useEffect(() => {
    if (!mapInstanceRef.current || !markersLayerRef.current || !clustersLayerRef.current) return;

    markersLayerRef.current.clearLayers();
    clustersLayerRef.current.clearLayers();

    if (!mapData || !mapData.locations || mapData.locations.length === 0) return;

    const bounds: L.LatLngExpression[] = [];

    // Render Regional Cluster Envelopes
    if (mapData.clusters) {
      mapData.clusters.forEach(cluster => {
        if (cluster.locationCount > 1) {
          const circle = L.circle([cluster.centroidLatitude, cluster.centroidLongitude], {
            radius: 25000, // 25 km
            color: '#8b5cf6',
            weight: 1.5,
            dashArray: '4, 8',
            fillColor: '#8b5cf6',
            fillOpacity: 0.08
          }).addTo(clustersLayerRef.current!);

          circle.bindTooltip(
            `<div style="font-size:11px;font-weight:600;color:#c084fc;">
              ${cluster.clusterLabel} (${cluster.locationCount} nodes)
            </div>`,
            { permanent: false, direction: 'top' }
          );
        }
      });
    }

    // Render Canonical Location Markers
    mapData.locations.forEach(loc => {
      bounds.push([loc.latitude, loc.longitude]);

      const precisionColor =
        loc.geocodePrecision === 'EXACT' || loc.geocodePrecision === 'BUILDING'
          ? '#10b981' // emerald
          : loc.geocodePrecision === 'STREET'
          ? '#3b82f6' // blue
          : '#f59e0b'; // amber/city

      const icon = L.divIcon({
        className: 'custom-geo-marker',
        html: `
          <div style="
            position: relative;
            display: flex;
            align-items: center;
            justify-content: center;
            width: 28px;
            height: 28px;
          ">
            <div style="
              position: absolute;
              width: 24px;
              height: 24px;
              background: ${precisionColor}25;
              border-radius: 50%;
              animation: ping 2s cubic-bezier(0, 0, 0.2, 1) infinite;
            "></div>
            <div style="
              width: 14px;
              height: 14px;
              background: ${precisionColor};
              border-radius: 50%;
              border: 2px solid #0f172a;
              box-shadow: 0 0 10px ${precisionColor}80;
              cursor: pointer;
            "></div>
          </div>
        `,
        iconSize: [28, 28],
        iconAnchor: [14, 14]
      });

      const marker = L.marker([loc.latitude, loc.longitude], { icon })
        .addTo(markersLayerRef.current!)
        .on('click', () => handleSelectLocation(loc));

      marker.bindTooltip(
        `<div style="
          background:#0f172a;
          border:1px solid #334155;
          color:#f8fafc;
          padding:6px 10px;
          border-radius:6px;
          font-size:11px;
          font-family:Inter,sans-serif;
        ">
          <strong style="color:${precisionColor}">${loc.name}</strong><br/>
          <span style="color:#94a3b8">${loc.city || ''} ${loc.state ? '· ' + loc.state : ''}</span><br/>
          <span style="color:#cbd5e1">${loc.eventCount} events · ${loc.entityCount} entities</span>
        </div>`,
        { permanent: false, sticky: true, direction: 'top' }
      );
    });

    if (bounds.length > 0) {
      mapInstanceRef.current.fitBounds(L.latLngBounds(bounds), { padding: [50, 50], maxZoom: 12 });
    }
  }, [mapData]);

  // Render Trajectory Polyline
  useEffect(() => {
    if (!mapInstanceRef.current || !trajectoryLayerRef.current) return;
    trajectoryLayerRef.current.clearLayers();

    if (!travelSequence || travelSequence.steps.length === 0) return;

    const latLngs: [number, number][] = travelSequence.steps.map(s => [s.latitude, s.longitude]);

    // Draw connecting path
    const polyline = L.polyline(latLngs, {
      color: '#38bdf8',
      weight: 3,
      opacity: 0.85,
      dashArray: '8, 8'
    }).addTo(trajectoryLayerRef.current);

    // Numbered step waypoints
    travelSequence.steps.forEach(step => {
      const stepColor = step.isImplausibleSpeed ? '#ef4444' : '#38bdf8';
      const stepIcon = L.divIcon({
        className: 'step-waypoint',
        html: `
          <div style="
            background:${stepColor};
            color:#0f172a;
            font-weight:700;
            font-size:11px;
            width:22px;
            height:22px;
            border-radius:50%;
            display:flex;
            align-items:center;
            justify-content:center;
            border:2px solid #ffffff;
            box-shadow:0 2px 8px rgba(0,0,0,0.5);
          ">
            ${step.stepIndex}
          </div>
        `,
        iconSize: [22, 22],
        iconAnchor: [11, 11]
      });

      L.marker([step.latitude, step.longitude], { icon: stepIcon })
        .addTo(trajectoryLayerRef.current!)
        .bindTooltip(
          `<div style="font-size:11px;padding:4px 8px;background:#0f172a;color:#fff;border:1px solid #334155;border-radius:4px;">
            <strong>Step ${step.stepIndex}: ${step.locationName}</strong><br/>
            <span>${new Date(step.timestampUtc).toLocaleDateString()} ${new Date(step.timestampUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span><br/>
            ${step.stepIndex > 1 ? `<span>+${step.distanceKmFromPrevious.toFixed(1)} km (${step.impliedSpeedKmh.toFixed(0)} km/h)</span>` : '<span>Origin</span>'}
          </div>`,
          { permanent: false, sticky: true, direction: 'top' }
        );
    });

    mapInstanceRef.current.fitBounds(polyline.getBounds(), { padding: [60, 60], maxZoom: 11 });
  }, [travelSequence]);

  // Render Proximity Radius Circle
  useEffect(() => {
    if (!mapInstanceRef.current || !proximityLayerRef.current) return;
    proximityLayerRef.current.clearLayers();

    if (!proximityCenter) return;

    L.circle(proximityCenter, {
      radius: proximityRadiusKm * 1000,
      color: '#06b6d4',
      weight: 2,
      fillColor: '#06b6d4',
      fillOpacity: 0.12
    }).addTo(proximityLayerRef.current);

    const centerIcon = L.divIcon({
      className: 'proximity-center-marker',
      html: `
        <div style="
          width:16px;height:16px;background:#06b6d4;border-radius:50%;
          border:3px solid #ffffff;box-shadow:0 0 12px #06b6d4;
        "></div>
      `,
      iconSize: [16, 16],
      iconAnchor: [8, 8]
    });

    L.marker(proximityCenter, { icon: centerIcon })
      .addTo(proximityLayerRef.current)
      .bindTooltip(`Proximity Search Origin (${proximityRadiusKm} km radius)`, { permanent: false, direction: 'top' });
  }, [proximityCenter, proximityRadiusKm]);

  return (
    <div className="flex flex-col gap-4 p-4 lg:p-6 min-h-screen bg-[#0b0f19] text-[#e2e8f0]" id="geospatial-map-page">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-3 border-b border-[var(--color-border)] pb-4">
        <div>
          <div className="flex items-center gap-2">
            <Compass className="w-6 h-6 text-[#10b981]" />
            <h1 className="text-xl font-bold tracking-tight text-white">
              Geospatial Intelligence & Investigation Map
            </h1>
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1 flex items-center gap-2">
            <span>Case Scoped:</span>
            <span className="text-[#38bdf8] font-semibold">{activeCaseNumber || effectiveCaseId}</span>
            <span>·</span>
            <span>{activeCaseTitle || 'Operation Hawala Network Investigation'}</span>
            <span>·</span>
            <span className="text-[#10b981] font-mono">Phase 7 Live GIS</span>
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleRunAnalysis}
            disabled={refreshing}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] border border-[var(--color-border)] text-xs font-medium rounded-md text-white transition disabled:opacity-50"
            title="Execute spatial-temporal overlap and clustering analysis"
          >
            <Play className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin' : 'text-[#38bdf8]'}`} />
            <span>Run Spatial Analysis</span>
          </button>

          <button
            onClick={() => {
              setRefreshing(true);
              loadMapData();
            }}
            disabled={refreshing}
            className="p-1.5 bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] border border-[var(--color-border)] rounded-md text-[var(--color-text-secondary)] hover:text-white transition"
            title="Refresh map"
          >
            <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {/* Responsible AI Disclaimer */}
      <ResponsibleAiNotice
        title="Responsible Geospatial Intelligence Notice"
        message="Spatial proximity and temporal overlaps are investigative signals indicating potential physical co-presence or regional activity, NOT proof of criminal association. Confirming a signal stages it for investigator notes and does not mutate or synthesize knowledge graph edges."
      />

      {/* Navigation Tabs */}
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--color-border)] pb-2">
        <div className="flex items-center gap-1 bg-[var(--color-surface-subtle)] p-1 rounded-lg border border-[var(--color-border)]">
          <button
            onClick={() => setActiveTab('MAP')}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium transition ${
              activeTab === 'MAP'
                ? 'bg-[#10b981] text-white shadow-sm'
                : 'text-[var(--color-text-secondary)] hover:text-white hover:bg-[var(--color-surface)]'
            }`}
          >
            <MapIcon className="w-3.5 h-3.5" />
            <span>Location Nodes ({mapData?.totalLocations || 0})</span>
          </button>

          <button
            onClick={() => setActiveTab('TRAJECTORY')}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium transition ${
              activeTab === 'TRAJECTORY'
                ? 'bg-[#38bdf8] text-white shadow-sm'
                : 'text-[var(--color-text-secondary)] hover:text-white hover:bg-[var(--color-surface)]'
            }`}
          >
            <Route className="w-3.5 h-3.5" />
            <span>Entity Trajectories</span>
          </button>

          <button
            onClick={() => setActiveTab('SIGNALS')}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium transition ${
              activeTab === 'SIGNALS'
                ? 'bg-[#f59e0b] text-white shadow-sm'
                : 'text-[var(--color-text-secondary)] hover:text-white hover:bg-[var(--color-surface)]'
            }`}
          >
            <Activity className="w-3.5 h-3.5" />
            <span>Spatial Signals ({signals.length})</span>
          </button>

          <button
            onClick={() => setActiveTab('PROXIMITY')}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-medium transition ${
              activeTab === 'PROXIMITY'
                ? 'bg-[#06b6d4] text-white shadow-sm'
                : 'text-[var(--color-text-secondary)] hover:text-white hover:bg-[var(--color-surface)]'
            }`}
          >
            <Crosshair className="w-3.5 h-3.5" />
            <span>Proximity Radius</span>
          </button>
        </div>

        {/* Legend / Metrics Pill */}
        <div className="flex items-center gap-4 text-xs text-[var(--color-text-secondary)]">
          <div className="flex items-center gap-1.5">
            <span className="w-2.5 h-2.5 rounded-full bg-[#10b981]"></span>
            <span>Exact / Building</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="w-2.5 h-2.5 rounded-full bg-[#3b82f6]"></span>
            <span>Street</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="w-2.5 h-2.5 rounded-full bg-[#f59e0b]"></span>
            <span>City / Area</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="w-2.5 h-2.5 rounded-full border border-dashed border-[#8b5cf6]"></span>
            <span>Cluster Envelope</span>
          </div>
        </div>
      </div>

      {/* Main Grid: Map & Interactive Panels */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
        {/* Map Container */}
        <div className={`col-span-12 ${selectedLocation || activeTab !== 'MAP' ? 'lg:col-span-8' : 'lg:col-span-12'} transition-all`}>
          <div className="relative rounded-xl overflow-hidden border border-[var(--color-border)] shadow-2xl bg-[#090d16]">
            {/* Map Canvas */}
            <div ref={mapContainerRef} className="w-full h-[620px]" style={{ zIndex: 1 }} />

            {/* Loading Overlay */}
            {loading && (
              <div className="absolute inset-0 bg-[#090d16]/80 backdrop-blur-sm z-10 flex flex-col items-center justify-center gap-2">
                <RefreshCw className="w-8 h-8 text-[#10b981] animate-spin" />
                <p className="text-xs text-[var(--color-text-secondary)]">Loading geospatial intelligence data...</p>
              </div>
            )}

            {/* Proximity Quick Tool Overlay */}
            {activeTab === 'PROXIMITY' && (
              <div className="absolute top-3 right-3 z-10 bg-[var(--color-surface-subtle)]/95 backdrop-blur-md border border-[var(--color-border)] p-3 rounded-lg shadow-xl w-72">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-xs font-semibold text-white flex items-center gap-1.5">
                    <Crosshair className="w-3.5 h-3.5 text-[#06b6d4]" />
                    Radius Filter
                  </span>
                  <span className="text-xs font-mono text-[#06b6d4]">{proximityRadiusKm} km</span>
                </div>
                <input
                  type="range"
                  min="5"
                  max="100"
                  step="5"
                  value={proximityRadiusKm}
                  onChange={e => {
                    const val = Number(e.target.value);
                    setProximityRadiusKm(val);
                    if (proximityCenter) {
                      runProximitySearch(proximityCenter[0], proximityCenter[1], val);
                    }
                  }}
                  className="w-full accent-[#06b6d4] cursor-pointer"
                />
                <p className="text-[10px] text-[var(--color-text-secondary)] mt-2">
                  Click any canonical location below or on the map to set the radius center.
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Side Panel: Conditional on active tab or selected location */}
        {(selectedLocation || activeTab !== 'MAP') && (
          <div className="col-span-12 lg:col-span-4 flex flex-col gap-4">
            {/* 1. Location Activity Drawer */}
            {selectedLocation && activeTab === 'MAP' && (
              <Card className="border border-[var(--color-border)] bg-[var(--color-surface-subtle)] shadow-xl p-4 flex flex-col gap-4 max-h-[620px] overflow-y-auto">
                <div className="flex items-start justify-between gap-2 border-b border-[var(--color-border)] pb-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <MapPin className="w-4 h-4 text-[#10b981]" />
                      <h3 className="font-semibold text-white text-sm">{selectedLocation.name}</h3>
                    </div>
                    <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">{selectedLocation.address || 'Address not indexed'}</p>
                    <div className="flex items-center gap-2 mt-1.5">
                      <span className="px-2 py-0.5 bg-[#10b981]/15 text-[#10b981] rounded text-[10px] font-mono uppercase">
                        {selectedLocation.geocodePrecision}
                      </span>
                      <span className="text-[10px] text-[var(--color-text-muted)] font-mono">
                        {selectedLocation.latitude.toFixed(4)}, {selectedLocation.longitude.toFixed(4)}
                      </span>
                    </div>
                  </div>
                  <button
                    onClick={() => setSelectedLocation(null)}
                    className="p-1 hover:bg-[var(--color-surface)] rounded text-[var(--color-text-secondary)] hover:text-white"
                  >
                    <X className="w-4 h-4" />
                  </button>
                </div>

                {/* Proximity Quick Trigger */}
                <div className="flex items-center justify-between bg-[var(--color-surface)]/50 p-2.5 rounded-lg border border-[var(--color-border)]">
                  <span className="text-xs text-[var(--color-text-secondary)]">Proximity Analysis</span>
                  <button
                    onClick={() => {
                      runProximitySearch(selectedLocation.latitude, selectedLocation.longitude, proximityRadiusKm);
                    }}
                    className="px-2.5 py-1 bg-[#06b6d4] hover:bg-[#0891b2] text-white text-xs font-medium rounded transition"
                  >
                    Search {proximityRadiusKm} km Radius
                  </button>
                </div>

                {/* Activity Feed */}
                {loadingActivity ? (
                  <div className="py-8 flex flex-col items-center justify-center gap-2 text-xs text-[var(--color-text-secondary)]">
                    <RefreshCw className="w-5 h-5 animate-spin text-[#10b981]" />
                    <span>Loading location events & observed entities...</span>
                  </div>
                ) : locationActivity ? (
                  <div className="flex flex-col gap-3">
                    {/* Entities Observed */}
                    <div>
                      <h4 className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wider mb-2 flex items-center gap-1.5">
                        <Activity className="w-3.5 h-3.5 text-[#38bdf8]" />
                        Observed Entities ({locationActivity.entities.length})
                      </h4>
                      <div className="flex flex-col gap-1.5 max-h-36 overflow-y-auto pr-1">
                        {locationActivity.entities.length === 0 ? (
                          <p className="text-xs text-[var(--color-text-muted)]">No entities formally extracted at this location yet.</p>
                        ) : (
                          locationActivity.entities.map(ent => (
                            <div
                              key={ent.entityId}
                              className="flex items-center justify-between p-2 rounded bg-[var(--color-surface)]/40 border border-[var(--color-border)]/50 text-xs"
                            >
                              <span className="font-medium text-white">{ent.entityName}</span>
                              <div className="flex items-center gap-2">
                                <span className="text-[10px] text-[var(--color-text-secondary)]">{ent.recordedVisits} visits</span>
                                <button
                                  onClick={() => {
                                    setSelectedEntityId(ent.entityId);
                                    loadTrajectory(ent.entityId);
                                  }}
                                  className="text-[#38bdf8] hover:underline text-[10px] flex items-center gap-0.5"
                                >
                                  Trajectory
                                </button>
                              </div>
                            </div>
                          ))
                        )}
                      </div>
                    </div>

                    {/* Associated Events */}
                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <h4 className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wider flex items-center gap-1.5">
                          <Clock className="w-3.5 h-3.5 text-[#f59e0b]" />
                          Timeline Events ({locationActivity.events.length})
                        </h4>
                        <button
                          onClick={() => navigate(`/timeline?location=${encodeURIComponent(selectedLocation.name)}`)}
                          className="text-[10px] text-[#38bdf8] hover:underline flex items-center gap-1"
                        >
                          View in Timeline <ExternalLink className="w-2.5 h-2.5" />
                        </button>
                      </div>

                      <div className="flex flex-col gap-1.5 max-h-48 overflow-y-auto pr-1">
                        {locationActivity.events.length === 0 ? (
                          <p className="text-xs text-[var(--color-text-muted)]">No timeline events linked to this coordinate.</p>
                        ) : (
                          locationActivity.events.map(ev => (
                            <div
                              key={ev.id}
                              className="p-2 rounded bg-[var(--color-surface)]/40 border border-[var(--color-border)]/50 text-xs flex flex-col gap-1"
                            >
                              <div className="flex items-center justify-between">
                                <span className="font-semibold text-white text-[11px]">{ev.eventType}</span>
                                <span className="text-[10px] text-[var(--color-text-secondary)]">
                                  {ev.startTimeUtc ? new Date(ev.startTimeUtc).toLocaleDateString() : 'Undated'}
                                </span>
                              </div>
                              <p className="text-[var(--color-text-secondary)] text-[11px] leading-relaxed">{ev.description}</p>
                            </div>
                          ))
                        )}
                      </div>
                    </div>

                    {/* Evidence Citations with SHA-256 */}
                    <div>
                      <h4 className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wider mb-2 flex items-center gap-1.5">
                        <FileText className="w-3.5 h-3.5 text-[#10b981]" />
                        Evidence Citations ({locationActivity.evidenceRecords.length})
                      </h4>
                      <div className="flex flex-col gap-1.5">
                        {locationActivity.evidenceRecords.map(evRec => (
                          <div
                            key={evRec.evidenceId}
                            className="p-2 rounded bg-[var(--color-surface)]/60 border border-[var(--color-border)] text-xs flex flex-col gap-1"
                          >
                            <div className="flex items-center justify-between">
                              <span className="font-medium text-white truncate max-w-[200px]">{evRec.fileName}</span>
                              <span className="flex items-center gap-1 text-[10px] text-[#10b981]">
                                <ShieldCheck className="w-3 h-3" />
                                SHA-256 Verified
                              </span>
                            </div>
                            <span className="font-mono text-[9px] text-[var(--color-text-muted)] truncate">{evRec.sha256Hash}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                ) : null}
              </Card>
            )}

            {/* 2. Trajectory View */}
            {activeTab === 'TRAJECTORY' && (
              <Card className="border border-[var(--color-border)] bg-[var(--color-surface-subtle)] shadow-xl p-4 flex flex-col gap-3 max-h-[620px] overflow-y-auto">
                <div className="border-b border-[var(--color-border)] pb-2">
                  <h3 className="font-semibold text-white text-sm flex items-center gap-1.5">
                    <Route className="w-4 h-4 text-[#38bdf8]" />
                    Entity Movement Trajectory
                  </h3>
                  <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">Chronological geospatial event sequence.</p>
                </div>

                <div className="flex items-center gap-2">
                  <input
                    type="text"
                    placeholder="Enter Entity Name or ID..."
                    value={selectedEntityId}
                    onChange={e => setSelectedEntityId(e.target.value)}
                    className="flex-1 bg-[var(--color-surface)] border border-[var(--color-border)] rounded px-2.5 py-1.5 text-xs text-white placeholder-[#64748b]"
                  />
                  <button
                    onClick={() => loadTrajectory(selectedEntityId)}
                    disabled={loadingTrajectory || !selectedEntityId}
                    className="px-3 py-1.5 bg-[#38bdf8] hover:bg-[#0284c7] text-white text-xs font-medium rounded transition disabled:opacity-50"
                  >
                    Trace
                  </button>
                </div>

                {loadingTrajectory ? (
                  <div className="py-8 flex flex-col items-center justify-center gap-2 text-xs text-[var(--color-text-secondary)]">
                    <RefreshCw className="w-5 h-5 animate-spin text-[#38bdf8]" />
                    <span>Calculating geodesic steps and travel velocity...</span>
                  </div>
                ) : travelSequence ? (
                  <div className="flex flex-col gap-3 mt-1">
                    <div className="grid grid-cols-2 gap-2 bg-[var(--color-surface)]/50 p-2.5 rounded-lg border border-[var(--color-border)]">
                      <div>
                        <span className="text-[10px] text-[var(--color-text-secondary)] block">Total Distance</span>
                        <span className="text-sm font-bold text-white">
                          {travelSequence.totalDistanceKm.toFixed(1)} km
                        </span>
                      </div>
                      <div>
                        <span className="text-[10px] text-[var(--color-text-secondary)] block">Waypoint Steps</span>
                        <span className="text-sm font-bold text-[#38bdf8]">{travelSequence.totalSteps}</span>
                      </div>
                    </div>

                    <div className="flex flex-col gap-2">
                      {travelSequence.steps.map((step, idx) => (
                        <div
                          key={step.eventId}
                          className={`p-2.5 rounded-lg border ${
                            step.isImplausibleSpeed
                              ? 'bg-[#ef4444]/10 border-[#ef4444]/40'
                              : 'bg-[var(--color-surface)]/40 border-[var(--color-border)]'
                          } flex flex-col gap-1 text-xs`}
                        >
                          <div className="flex items-center justify-between">
                            <span className="font-semibold text-white flex items-center gap-1.5">
                              <span className="w-4 h-4 rounded-full bg-[#38bdf8] text-[#0f172a] font-bold text-[10px] flex items-center justify-center">
                                {step.stepIndex}
                              </span>
                              {step.locationName}
                            </span>
                            <span className="text-[10px] text-[var(--color-text-secondary)]">
                              {new Date(step.timestampUtc).toLocaleDateString()}{' '}
                              {new Date(step.timestampUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                            </span>
                          </div>

                          <p className="text-[11px] text-[var(--color-text-secondary)]">{step.description}</p>

                          {idx > 0 && (
                            <div className="flex items-center justify-between pt-1 border-t border-[var(--color-border)]/50 text-[10px] text-[var(--color-text-secondary)]">
                              <span>+{step.distanceKmFromPrevious.toFixed(1)} km</span>
                              <span>{step.elapsedFormatted}</span>
                              <span className={step.isImplausibleSpeed ? 'text-[#ef4444] font-bold' : ''}>
                                {step.impliedSpeedKmh.toFixed(0)} km/h
                              </span>
                            </div>
                          )}

                          {step.isImplausibleSpeed && (
                            <div className="flex items-center gap-1.5 text-[10px] text-[#ef4444] bg-[#ef4444]/15 px-2 py-1 rounded">
                              <AlertTriangle className="w-3 h-3 flex-shrink-0" />
                              <span>Implausible velocity warning: exceeds 900 km/h flight baseline. Verify timestamps.</span>
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <p className="text-xs text-[var(--color-text-muted)] text-center py-6">
                    Enter an entity identifier above to visualize their chronological trajectory across locations.
                  </p>
                )}
              </Card>
            )}

            {/* 3. Spatial Signals Review */}
            {activeTab === 'SIGNALS' && (
              <Card className="border border-[var(--color-border)] bg-[var(--color-surface-subtle)] shadow-xl p-4 flex flex-col gap-3 max-h-[620px] overflow-y-auto">
                <div className="border-b border-[var(--color-border)] pb-2 flex items-center justify-between">
                  <div>
                    <h3 className="font-semibold text-white text-sm flex items-center gap-1.5">
                      <Activity className="w-4 h-4 text-[#f59e0b]" />
                      Spatial Signals Review
                    </h3>
                    <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">Staged geographic correlation leads.</p>
                  </div>
                  <select
                    value={signalStatusFilter}
                    onChange={e => setSignalStatusFilter(e.target.value)}
                    className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded px-2 py-1 text-xs text-white"
                  >
                    <option value="ALL">All Status</option>
                    <option value="PENDING">Pending</option>
                    <option value="CONFIRMED">Confirmed</option>
                    <option value="DISMISSED">Dismissed</option>
                  </select>
                </div>

                <div className="flex flex-col gap-2.5">
                  {signals
                    .filter(s => signalStatusFilter === 'ALL' || s.status === signalStatusFilter)
                    .map(sig => (
                      <div
                        key={sig.id}
                        className="p-3 rounded-lg bg-[var(--color-surface)]/60 border border-[var(--color-border)] flex flex-col gap-2 text-xs"
                      >
                        <div className="flex items-center justify-between">
                          <span className="font-semibold text-white">{sig.signalType.replace(/_/g, ' ')}</span>
                          <span
                            className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                              sig.status === 'CONFIRMED'
                                ? 'bg-[#10b981]/20 text-[#10b981]'
                                : sig.status === 'DISMISSED'
                                ? 'bg-[#64748b]/20 text-[var(--color-text-secondary)]'
                                : 'bg-[#f59e0b]/20 text-[#f59e0b]'
                            }`}
                          >
                            {sig.status}
                          </span>
                        </div>

                        <p className="text-[11px] text-[var(--color-text-secondary)] leading-relaxed">{sig.explanation}</p>

                        <div className="flex items-center justify-between text-[10px] text-[var(--color-text-secondary)] pt-1 border-t border-[var(--color-border)]">
                          <span>Confidence Score: {sig.score.toFixed(2)}</span>
                          {sig.status === 'PENDING' && (
                            <button
                              onClick={() => setReviewingSignal(sig)}
                              className="px-2 py-1 bg-[#334155] hover:bg-[#475569] text-white rounded font-medium transition"
                            >
                              Review Lead
                            </button>
                          )}
                        </div>
                      </div>
                    ))}

                  {signals.length === 0 && (
                    <p className="text-xs text-[var(--color-text-muted)] text-center py-6">
                      No spatial signals detected yet. Click "Run Spatial Analysis" above to detect co-presence and velocity leads.
                    </p>
                  )}
                </div>

                {/* Review Modal Dialog */}
                {reviewingSignal && (
                  <div className="fixed inset-0 bg-black/70 backdrop-blur-sm z-50 flex items-center justify-center p-4">
                    <div className="bg-[var(--color-surface-subtle)] border border-[var(--color-border)] rounded-xl max-w-md w-full p-5 shadow-2xl flex flex-col gap-4">
                      <div className="flex items-center justify-between border-b border-[var(--color-border)] pb-2">
                        <h4 className="font-bold text-white text-sm flex items-center gap-1.5">
                          <ShieldCheck className="w-4 h-4 text-[#10b981]" />
                          Review Spatial Investigative Lead
                        </h4>
                        <button
                          onClick={() => setReviewingSignal(null)}
                          className="text-[var(--color-text-secondary)] hover:text-white"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>

                      <p className="text-xs text-[var(--color-text-secondary)] bg-[var(--color-surface)]/50 p-2.5 rounded border border-[var(--color-border)]">
                        {reviewingSignal.explanation}
                      </p>

                      <div>
                        <label className="block text-[11px] text-[var(--color-text-secondary)] font-medium mb-1">
                          Investigator Review Notes (Required for Audit Trail)
                        </label>
                        <textarea
                          rows={3}
                          value={reviewNotes}
                          onChange={e => setReviewNotes(e.target.value)}
                          placeholder="State justification or corroborating evidence logs..."
                          className="w-full bg-[var(--color-surface)] border border-[var(--color-border)] rounded p-2 text-xs text-white placeholder-[#64748b] focus:outline-none focus:border-[#10b981]"
                        />
                      </div>

                      <div className="p-2.5 rounded bg-[#f59e0b]/10 border border-[#f59e0b]/30 text-[10px] text-[#f59e0b]">
                        Note: Confirming marks this signal as verified in investigative dossiers. It does NOT automatically mutate or insert edges in the knowledge graph.
                      </div>

                      <div className="flex items-center justify-end gap-2 pt-2 border-t border-[var(--color-border)]">
                        <button
                          onClick={() => handleReviewSignal('DISMISSED')}
                          disabled={submittingReview}
                          className="px-3 py-1.5 bg-[#334155] hover:bg-[#475569] text-white rounded text-xs font-medium transition"
                        >
                          Dismiss Lead
                        </button>
                        <button
                          onClick={() => handleReviewSignal('CONFIRMED')}
                          disabled={submittingReview}
                          className="px-3 py-1.5 bg-[#10b981] hover:bg-[#059669] text-white rounded text-xs font-medium transition flex items-center gap-1.5"
                        >
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          Confirm Lead
                        </button>
                      </div>
                    </div>
                  </div>
                )}
              </Card>
            )}

            {/* 4. Proximity Results Drawer */}
            {activeTab === 'PROXIMITY' && (
              <Card className="border border-[var(--color-border)] bg-[var(--color-surface-subtle)] shadow-xl p-4 flex flex-col gap-3 max-h-[620px] overflow-y-auto">
                <div className="border-b border-[var(--color-border)] pb-2">
                  <h3 className="font-semibold text-white text-sm flex items-center gap-1.5">
                    <Crosshair className="w-4 h-4 text-[#06b6d4]" />
                    Proximity Search Results
                  </h3>
                  <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">
                    Nodes within {proximityRadiusKm} km of selected coordinate.
                  </p>
                </div>

                <div className="flex flex-col gap-2">
                  {proximityResults.map(match => (
                    <div
                      key={match.locationId}
                      onClick={() => {
                        const fullLoc = mapData?.locations.find(l => l.id === match.locationId);
                        if (fullLoc) handleSelectLocation(fullLoc);
                      }}
                      className="p-2.5 rounded-lg bg-[var(--color-surface)]/60 border border-[var(--color-border)] hover:border-[#06b6d4] transition cursor-pointer flex flex-col gap-1 text-xs"
                    >
                      <div className="flex items-center justify-between">
                        <span className="font-semibold text-white">{match.name}</span>
                        <span className="text-[#06b6d4] font-mono font-bold text-[11px]">
                          {match.distanceKm.toFixed(1)} km
                        </span>
                      </div>
                      <div className="flex items-center justify-between text-[10px] text-[var(--color-text-secondary)]">
                        <span>{match.city || 'Regional Hub'}</span>
                        <span>{match.eventCount} events · {match.entityCount} entities</span>
                      </div>
                    </div>
                  ))}

                  {proximityResults.length === 0 && (
                    <p className="text-xs text-[var(--color-text-muted)] text-center py-6">
                      Click any location on the map or use the slider to execute an exact Haversine radius search.
                    </p>
                  )}
                </div>
              </Card>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
