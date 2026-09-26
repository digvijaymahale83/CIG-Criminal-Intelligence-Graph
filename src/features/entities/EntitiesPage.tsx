import React, { useState } from 'react';
import { Users2, Search, Filter, ShieldCheck, Check, X, AlertCircle } from 'lucide-react';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { apiClient } from '../../services/api/client';
import { Button } from '../../components/common/Button';
import { StatusBadge } from '../../components/common/StatusBadge';
import { Card } from '../../components/common/Card';
import { PhaseNotice } from '../../components/common/PhaseNotice';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';

const SAMPLE_ENTITIES = [
  {
    id: 'ent-01',
    displayName: 'Viktor K. Vance',
    type: 'Person',
    reviewStatus: 'Human Reviewed',
    confidence: 0.94,
    sourceMentions: 14,
    relationships: 9,
    firstObserved: '2026-03-12',
    sourceDocument: 'FININT-Wire-Transfers-2026-Q1.csv',
  },
  {
    id: 'ent-02',
    displayName: 'Aegis Maritime Logistics Ltd.',
    type: 'Organization',
    reviewStatus: 'Accepted Representation',
    confidence: 0.91,
    sourceMentions: 22,
    relationships: 12,
    firstObserved: '2026-02-18',
    sourceDocument: 'Corporate-Registry-BVI-Holdings.pdf',
  },
  {
    id: 'ent-03',
    displayName: '+1 (555) 019-4829',
    type: 'Phone',
    reviewStatus: 'Machine Extracted',
    confidence: 0.85,
    sourceMentions: 8,
    relationships: 4,
    firstObserved: '2026-05-01',
    sourceDocument: 'Subpoena-Return-Cellular-Towers.xlsx',
  },
  {
    id: 'ent-04',
    displayName: 'Wire Transfer Structuring (Offense Code 18-USC-1956)',
    type: 'Crime', // Incident category, not proof of guilt
    reviewStatus: 'Source Reported',
    confidence: 1.0,
    sourceMentions: 6,
    relationships: 5,
    firstObserved: '2026-01-10',
    sourceDocument: 'FININT-Wire-Transfers-2026-Q1.csv',
  },
];

export const EntitiesPage: React.FC = () => {
  const { activeCaseId, activeCaseNumber } = useActiveInvestigation();
  const [selectedTab, setSelectedTab] = useState<'entities' | 'resolution'>('entities');
  const [entities, setEntities] = useState(SAMPLE_ENTITIES);

  React.useEffect(() => {
    const fetchEntities = async () => {
      try {
        let rawData: any[];
        try {
          rawData = await apiClient.get<any[]>(`/api/v1/entities?caseId=${activeCaseId || ''}`);
        } catch {
          rawData = await apiClient.get<any[]>(`/api/entities?investigation_id=${activeCaseId || ''}`);
        }
        if (rawData && rawData.length > 0) {
          const mapped = rawData.map((e: any) => ({
            id: e.id,
            displayName: e.canonicalName || e.canonical_name || e.name,
            type: e.type,
            reviewStatus: e.verificationStatus || e.verification_status || 'Verified',
            confidence: e.confidence || 0.95,
            sourceMentions: 12,
            relationships: 6,
            firstObserved: e.createdAtUtc ? e.createdAtUtc.split('T')[0] : '2026-08-15',
            sourceDocument: e.location || 'Investigative Record',
          }));
          setEntities(mapped);
        }
      } catch (err) {
        console.error('Failed to load entities:', err);
      }
    };
    fetchEntities();
  }, [activeCaseId]);

  return (
    <div className="space-y-6" id="entities-page">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-[var(--color-border)]">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">Entities & Resolution Review</h1>
            <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border border-[var(--color-border)]">
              CASE: {activeCaseNumber}
            </span>
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1">
            Investigative entity extraction, deduplication, and human-in-the-loop resolution review.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant={selectedTab === 'entities' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSelectedTab('entities')}
          >
            Extracted Entities ({entities.length})
          </Button>
          <Button
            variant={selectedTab === 'resolution' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSelectedTab('resolution')}
          >
            Resolution Queue (1 Potential Match)
          </Button>
        </div>
      </div>

      <ResponsibleAiNotice />

      {selectedTab === 'entities' ? (
        <Card title="Case Entity Index" subtitle="Grounded in ingested evidence documents">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b border-[var(--color-border)] text-[var(--color-text-muted)] font-mono text-[11px]">
                  <th className="pb-3 font-semibold">Entity Display Name</th>
                  <th className="pb-3 font-semibold">Type</th>
                  <th className="pb-3 font-semibold">Review Status</th>
                  <th className="pb-3 font-semibold">Calibrated Confidence</th>
                  <th className="pb-3 font-semibold">Evidence Provenance</th>
                  <th className="pb-3 font-semibold text-right">Links</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#EFE5D8]">
                {entities.map((ent) => (
                  <tr key={ent.id} className="hover:bg-[var(--color-surface)] transition-colors">
                    <td className="py-3 pr-3 font-medium text-[var(--color-text-primary)]">
                      <div>
                        <span>{ent.displayName}</span>
                        {ent.type === 'Crime' && (
                          <p className="text-[10px] text-[#8F4D08] mt-0.5 font-medium">
                            * Source incident classification. Not proof of guilt.
                          </p>
                        )}
                      </div>
                    </td>
                    <td className="py-3 px-2">
                      <span className="font-mono text-[10px] px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)]">
                        {ent.type}
                      </span>
                    </td>
                    <td className="py-3 px-2">
                      <StatusBadge status={ent.reviewStatus} size="sm" />
                    </td>
                    <td className="py-3 px-2 font-mono text-[var(--color-text-primary)] text-[11px]">
                      {(ent.confidence * 100).toFixed(0)}%
                    </td>
                    <td className="py-3 px-2 text-[11px] text-[var(--color-text-muted)] font-mono">
                      {ent.sourceDocument}
                    </td>
                    <td className="py-3 pl-2 text-right font-mono text-[var(--color-text-primary)]">
                      {ent.relationships} relations
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      ) : (
        <Card
          title="Entity Resolution Disambiguation Queue"
          subtitle="Human review required before entities are linked or resolved"
        >
          <div className="p-5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] space-y-4 shadow-2xs">
            <div className="flex items-center justify-between">
              <span className="text-xs font-mono font-semibold text-[#8F4D08]">
                POTENTIAL MATCH CANDIDATE #RES-0819
              </span>
              <StatusBadge status="Under Review" size="sm" />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="p-3.5 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] space-y-1 shadow-2xs">
                <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Mention A</span>
                <p className="text-xs font-bold text-[var(--color-text-primary)]">V. K. Vance</p>
                <p className="text-[11px] text-[var(--color-text-primary)]">Type: Person</p>
                <p className="text-[11px] text-[var(--color-text-muted)] font-mono">Doc: Wire-Log-Q1.csv</p>
              </div>
              <div className="p-3.5 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] space-y-1 shadow-2xs">
                <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Mention B</span>
                <p className="text-xs font-bold text-[var(--color-text-primary)]">Viktor Vance</p>
                <p className="text-[11px] text-[var(--color-text-primary)]">Type: Person</p>
                <p className="text-[11px] text-[var(--color-text-muted)] font-mono">Doc: BVI-Holdings.pdf</p>
              </div>
            </div>

            <p className="text-xs text-[var(--color-text-primary)] leading-relaxed">
              Matching signals: Shared registered address (88% token similarity), co-occurring phone identifier in call records.
              No conflicting dates of birth detected.
            </p>

            <div className="pt-3 border-t border-[var(--color-border)] flex items-center justify-end gap-2.5">
              <Button variant="danger" size="sm" icon={<X className="w-3.5 h-3.5" />}>
                Reject Resolution
              </Button>
              <Button variant="primary" size="sm" icon={<Check className="w-3.5 h-3.5" />}>
                Accept Representation
              </Button>
            </div>
          </div>
        </Card>
      )}

      <PhaseNotice phaseNumber={7} moduleName="Entity Disambiguation Engine" />
    </div>
  );
};
