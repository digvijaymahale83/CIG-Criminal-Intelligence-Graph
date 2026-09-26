import { apiClient } from '../api/client';

export interface EvidenceLedgerBlock {
  id: string;
  blockIndex: number;
  evidenceItemId?: string;
  evidenceVersion: number;
  evidenceHash: string;
  previousBlockHash: string;
  blockHash: string;
  action: string;
  actorUserId: string;
  actorName: string;
  timestampUtc: string;
  metadataJson?: string;
  createdAtUtc: string;
}

export type IntegrityStatusType =
  | 'VERIFIED'
  | 'EVIDENCE_MODIFIED'
  | 'LEDGER_MISMATCH'
  | 'CHAIN_INVALID'
  | 'MISSING_FILE'
  | 'MISSING_LEDGER_RECORD'
  | 'HASH_MISMATCH';

export interface EvidenceIntegrityStatus {
  evidenceId: string;
  fileName: string;
  version: number;
  actualFileSha256: string;
  registeredSha256: string;
  ledgerSha256: string;
  status: IntegrityStatusType;
  chainStatus: 'VALID' | 'BROKEN' | 'UNKNOWN';
  blockIndex?: number;
  action?: string;
  actorName?: string;
  verifiedAtUtc: string;
  explanation: string;
}

export interface ChainValidationResult {
  isValid: boolean;
  totalBlocks: number;
  checkedBlocks: number;
  firstInvalidBlockIndex?: number;
  failureReason?: string;
  validatedAtUtc: string;
}

export interface ReconciliationItem {
  evidenceId: string;
  caseId?: string;
  fileName: string;
  version: number;
  sha256Hash: string;
  uploadedAtUtc: string;
  uploadedByName: string;
  status: string;
}

export class IntegrityService {
  /**
   * Retrieves fast stored integrity status for an evidence item
   */
  public static async getEvidenceIntegrity(evidenceId: string): Promise<EvidenceIntegrityStatus> {
    return await apiClient.get<EvidenceIntegrityStatus>(`/api/v1/evidence/${evidenceId}/integrity`);
  }

  /**
   * Retrieves append-only block history for a specific evidence item
   */
  public static async getEvidenceHistory(evidenceId: string): Promise<EvidenceLedgerBlock[]> {
    return await apiClient.get<EvidenceLedgerBlock[]>(`/api/v1/evidence/${evidenceId}/integrity/history`);
  }

  /**
   * Performs live byte-level re-verification against actual disk file
   */
  public static async verifyEvidenceNow(evidenceId: string): Promise<EvidenceIntegrityStatus> {
    return await apiClient.post<EvidenceIntegrityStatus>(`/api/v1/evidence/${evidenceId}/integrity/verify`, {});
  }

  /**
   * Retrieves paginated ledger blocks from the append-only ledger
   */
  public static async getLedgerBlocks(limit: number = 50, offset: number = 0): Promise<EvidenceLedgerBlock[]> {
    return await apiClient.get<EvidenceLedgerBlock[]>(`/api/v1/integrity/ledger?limit=${limit}&offset=${offset}`);
  }

  /**
   * Retrieves single block details by block index
   */
  public static async getBlockByIndex(blockIndex: number): Promise<EvidenceLedgerBlock> {
    return await apiClient.get<EvidenceLedgerBlock>(`/api/v1/integrity/ledger/${blockIndex}`);
  }

  /**
   * Executes complete cryptographic chain validation from Genesis to latest block
   */
  public static async verifyChain(): Promise<ChainValidationResult> {
    return await apiClient.post<ChainValidationResult>('/api/v1/integrity/ledger/verify-chain', {});
  }

  /**
   * Retrieves unledgered evidence items pending registration
   */
  public static async getReconciliationCandidates(): Promise<ReconciliationItem[]> {
    return await apiClient.get<ReconciliationItem[]>('/api/v1/integrity/reconciliation');
  }

  /**
   * Registers a missing evidence ledger entry
   */
  public static async reconcileEvidence(evidenceId: string): Promise<EvidenceLedgerBlock> {
    return await apiClient.post<EvidenceLedgerBlock>(`/api/v1/integrity/reconciliation/${evidenceId}`, {});
  }
}
