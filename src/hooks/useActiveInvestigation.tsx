import React, { createContext, useContext, useEffect, useState } from 'react';

export interface ActiveInvestigationContextType {
  activeInvestigationId: string | null;
  activeCaseId: string | null;
  activeCaseNumber: string | null;
  activeCaseTitle: string | null;
  setActiveInvestigation: (id: string, caseNumber: string, title: string) => void;
  clearActiveInvestigation: () => void;
}

const ActiveInvestigationContext = createContext<ActiveInvestigationContextType | undefined>(undefined);

// Initial default active investigation for investigator workspace context
const DEFAULT_INVESTIGATION = {
  id: 'inv-2026-0841',
  caseNumber: 'CASE-2026-0841-ORG',
  title: 'Operation Iron Vault Syndicate',
};

export const ActiveInvestigationProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [activeInvestigationId, setActiveId] = useState<string | null>(DEFAULT_INVESTIGATION.id);
  const [activeCaseNumber, setCaseNumber] = useState<string | null>(DEFAULT_INVESTIGATION.caseNumber);
  const [activeCaseTitle, setCaseTitle] = useState<string | null>(DEFAULT_INVESTIGATION.title);

  // Sync to sessionStorage to avoid losing context during page refreshes while avoiding leakage
  useEffect(() => {
    const savedId = sessionStorage.getItem('activeInvestigationId');
    const savedNumber = sessionStorage.getItem('activeCaseNumber');
    const savedTitle = sessionStorage.getItem('activeCaseTitle');

    if (savedId && savedNumber && savedTitle) {
      setActiveId(savedId);
      setCaseNumber(savedNumber);
      setCaseTitle(savedTitle);
    }
  }, []);

  const setActiveInvestigation = (id: string, caseNumber: string, title: string) => {
    setActiveId(id);
    setCaseNumber(caseNumber);
    setCaseTitle(title);
    sessionStorage.setItem('activeInvestigationId', id);
    sessionStorage.setItem('activeCaseNumber', caseNumber);
    sessionStorage.setItem('activeCaseTitle', title);
  };

  const clearActiveInvestigation = () => {
    setActiveId(null);
    setCaseNumber(null);
    setCaseTitle(null);
    sessionStorage.removeItem('activeInvestigationId');
    sessionStorage.removeItem('activeCaseNumber');
    sessionStorage.removeItem('activeCaseTitle');
  };

  return (
    <ActiveInvestigationContext.Provider
      value={{
        activeInvestigationId,
        activeCaseId: activeInvestigationId,
        activeCaseNumber,
        activeCaseTitle,
        setActiveInvestigation,
        clearActiveInvestigation,
      }}
    >
      {children}
    </ActiveInvestigationContext.Provider>
  );
};

export function useActiveInvestigation() {
  const context = useContext(ActiveInvestigationContext);
  if (!context) {
    throw new Error('useActiveInvestigation must be used within an ActiveInvestigationProvider');
  }
  return context;
}
