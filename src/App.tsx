import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './hooks/useAuth';
import { ThemeProvider } from './hooks/useTheme';
import { TranslationProvider } from './i18n';
import { ActiveInvestigationProvider } from './hooks/useActiveInvestigation';
import { AppShell } from './components/layout/AppShell';

// Feature Pages
import { DashboardPage } from './features/dashboard/DashboardPage';
import { SystemStatusPage } from './features/system-status/SystemStatusPage';
import { InvestigationsPage } from './features/investigations/InvestigationsPage';
import { NewInvestigationPage } from './features/investigations/NewInvestigationPage';
import { EvidenceListPage } from './features/evidence/EvidenceListPage';
import { EvidenceDetailPage } from './features/evidence/EvidenceDetailPage';
import { ExtractionReviewPage } from './features/evidence/ExtractionReviewPage';
import { EntitiesPage } from './features/entities/EntitiesPage';
import { EntityResolutionPage } from './features/entity-resolution/EntityResolutionPage';
import { NetworkGraphPage } from './features/graph/NetworkGraphPage';
import { AnalyticsPage } from './features/analytics/AnalyticsPage';
import { AnomaliesPage } from './features/anomalies/AnomaliesPage';
import { TimelinePage } from './features/timeline/TimelinePage';
import { GeospatialMapPage } from './features/map/GeospatialMapPage';
import { AssistantPage } from './features/assistant/AssistantPage';
import { ReportsPage } from './features/reports/ReportsPage';
import { AuditLogPage } from './features/audit/AuditLogPage';
import { SettingsPage } from './features/settings/SettingsPage';
import { LoginPage } from './features/auth/LoginPage';

// Create TanStack Query Client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <TranslationProvider>
          <AuthProvider>
            <ActiveInvestigationProvider>
              <BrowserRouter>
                <Routes>
                  {/* Standalone Authentication Route */}
                  <Route path="/login" element={<LoginPage />} />

                  {/* Main Application Shell with Case Context & Navigation */}
                  <Route element={<AppShell />}>
                    <Route index element={<Navigate to="/dashboard" replace />} />
                    <Route path="/dashboard" element={<DashboardPage />} />
                    <Route path="/cases/:caseId/dashboard" element={<DashboardPage />} />
                    <Route path="/system-status" element={<SystemStatusPage />} />
                    <Route path="/investigations" element={<InvestigationsPage />} />
                    <Route path="/investigations/new" element={<NewInvestigationPage />} />
                    <Route path="/evidence" element={<EvidenceListPage />} />
                    <Route path="/evidence/:evidenceId" element={<EvidenceDetailPage />} />
                    <Route path="/evidence/:evidenceId/extraction" element={<ExtractionReviewPage />} />
                    <Route path="/entities" element={<EntitiesPage />} />
                    <Route path="/entity-resolution" element={<EntityResolutionPage />} />
                    <Route path="/network" element={<NetworkGraphPage />} />
                    <Route path="/analytics" element={<AnalyticsPage />} />
                    <Route path="/anomalies" element={<AnomaliesPage />} />
                    <Route path="/alerts" element={<AnomaliesPage />} />
                    <Route path="/cases/:caseId/alerts" element={<AnomaliesPage />} />
                    <Route path="/cases/:caseId/anomalies" element={<AnomaliesPage />} />
                    <Route path="/timeline" element={<TimelinePage />} />
                    <Route path="/map" element={<GeospatialMapPage />} />
                    <Route path="/assistant" element={<AssistantPage />} />
                    <Route path="/copilot" element={<AssistantPage />} />
                    <Route path="/reports" element={<ReportsPage />} />
                    <Route path="/audit" element={<AuditLogPage />} />
                    <Route path="/settings" element={<SettingsPage />} />

                    {/* Catch-all fallback */}
                    <Route path="*" element={<Navigate to="/dashboard" replace />} />
                  </Route>
                </Routes>
              </BrowserRouter>
            </ActiveInvestigationProvider>
          </AuthProvider>
        </TranslationProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
