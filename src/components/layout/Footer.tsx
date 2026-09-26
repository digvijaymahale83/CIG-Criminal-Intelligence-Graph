import React from 'react';
import { useAuth } from '../../hooks/useAuth';

export const Footer: React.FC = () => {
  const { user } = useAuth();
  const operatorCode = user?.badgeNumber
    ? `AUTH: ${user.badgeNumber.replace('-', '_')}`
    : 'AUTH: OP_0001_ADMIN';

  return (
    <footer
      id="app-bottom-footer"
      className="h-9 border-t border-[#21262d] bg-[#0d1117] flex items-center px-4 sm:px-6 justify-between shrink-0 font-mono text-[10px] text-[#484f58]"
    >
      <div className="flex items-center gap-3 sm:gap-4">
        <span>PROTOCOL: SECURE-TLS v1.3</span>
        <span className="h-3 w-[1px] bg-[#21262d] hidden sm:inline-block" />
        <span className="hidden sm:inline-block">{operatorCode}</span>
      </div>
      <div className="flex items-center gap-2 sm:gap-4 uppercase tracking-tighter">
        <span className="text-[#8b949e]">The Criminal Intelligence & Network Investigation Platform · Synthetic Demo</span>
        <span className="text-[#21262d] hidden sm:inline">•</span>
        <span className="hidden sm:inline">v4.0.0-research</span>
      </div>
    </footer>
  );
};
