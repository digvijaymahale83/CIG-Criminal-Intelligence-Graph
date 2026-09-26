# Model Card: Graph Attention Network (GAT) for Investigative Network Analysis

## 1. Model Details

- **Model Identifier:** `gat-link-prediction-v1`
- **Architecture:** 2-Layer Graph Attention Network (GAT) with multi-head self-attention
- **Framework:** PyTorch & PyTorch Geometric (`torch_geometric.nn.GATConv` with fallback to `PurePyTorchGATConv`)
- **Input Node Dimension:** 19-dimensional continuous and one-hot categorical feature vector
- **Hidden Feature Dimension:** 32 dimensions (4 attention heads × 8 dimensions per head)
- **Output Embedding Dimension:** 32 dimensions per node (L2 normalized)
- **Activation Functions:** ELU (Exponential Linear Unit) between layers, L2-normalization on final embeddings
- **Task:** Link Prediction / Latent Edge Discovery in heterogeneous investigation graphs

---

## 2. Intended Use & Target Users

- **Primary Intended Use:** Investigative decision support and discovery of non-obvious entity associations across synthetic/demo investigation case records for SIH and academic research.
- **Target Users:** Intelligence analysts, case investigators, and network research teams.
- **Workflow Role:** Suggests potential relationship hypotheses (`MODEL-GENERATED LEAD`) for explicit human verification.
- **Governance Mandate:** The model operates strictly in a *human-in-the-loop* advisory capacity. Under no circumstances does the model directly mutate, create, or alter verified case intelligence without an investigator's recorded decision.

---

## 3. Out-of-Scope & Prohibited Uses

- **Autonomous Actions:** The model must never be used to make autonomous legal, operational, or surveillance determinations.
- **Predictive Policing:** The model must never be deployed for predictive policing, guilt scoring, risk profiling, or automated suspicion algorithms.
- **Evidence Generation:** Model-generated leads are investigative hypotheses and do not constitute legal evidence in court.
- **Pejorative Labeling:** Entities must never be classified as "criminals", "offenders", "kingpins", or "guilty" based on graph metrics or ML outputs. Only neutral terminology is permitted:
  - *Key Nexus Entity* (high centrality)
  - *High Connectivity Lead* (high degree / betweenness)
  - *Association Cluster* (community detection)
  - *Potential Relationship / Investigative Lead* (link prediction)

---

## 4. Input Representation & Feature Engineering

Each entity in the graph is represented as a deterministic 19-dimensional feature vector:

| Index | Feature | Type | Description |
|:---:|:---|:---:|:---|
| 0–6 | Entity Type One-Hot | Categorical (7) | One-hot encoded across: `Person`, `Organization`, `Location`, `Phone`, `BankAccount`, `Vehicle`, `Other` |
| 7 | Cross-Case Flag | Binary (0/1) | 1.0 if entity is referenced across multiple case dockets |
| 8 | Degree Centrality | Continuous | Normalized degree centrality within the case graph |
| 9 | In-Degree Centrality | Continuous | Normalized in-degree centrality |
| 10 | Out-Degree Centrality | Continuous | Normalized out-degree centrality |
| 11 | Local Clustering Coefficient | Continuous | Clustering coefficient of the node's ego-network |
| 12 | Neighbor Count | Integer (Norm) | Number of direct 1-hop neighbors |
| 13 | Cross-Case Frequency | Integer (Norm) | Frequency count across all system investigations |
| 14 | Associated Evidence Count | Integer (Norm) | Count of evidentiary documents mentioning this entity |
| 15–18 | Metadata Salience Flags | Binary (4) | Identification presence (phone digits, account digits, address completeness, registration) |

---

## 5. Scoring & Explainability Pipeline

Link prediction candidates are scored using a deterministic, multi-signal composite function:

$$\text{Score}(u, v) = 0.50 \cdot S_{\cos}(\mathbf{z}_u, \mathbf{z}_v) + 0.30 \cdot S_{\text{shared}}(u, v) + 0.20 \cdot \alpha_{u, v}$$

Where:
1. **$S_{\cos}(\mathbf{z}_u, \mathbf{z}_v)$**: Cosine similarity between GAT-learned node embeddings $\mathbf{z}_u$ and $\mathbf{z}_v$, mapped to $[0, 1]$ via $(1 + \cos(\mathbf{z}_u, \mathbf{z}_v)) / 2$.
2. **$S_{\text{shared}}(u, v)$**: Common-neighbor overlap normalized by Jaccard / degree coefficients.
3. **$\alpha_{u, v}$**: Graph attention coefficient from the top-layer attention mechanism.

### Signal Breakdown for Investigators
Every model lead provides a transparent breakdown:
- **Cosine Similarity:** Similarity score in the latent structural feature space.
- **Shared Intermediaries:** Exact list and count of mutual entity contacts connecting both nodes.
- **Attention Weight:** Multi-head attention focus between local neighborhoods.
- **Weighted Contributions:** Clear percentage contributions indicating why the model surfaced the candidate.

---

## 6. Training & Evaluation Methodology

- **Training Strategy:** Self-supervised link prediction with negative edge sampling (1:1 positive-to-negative ratio). Binary Cross-Entropy Loss with positive weight regularization:
  $$\mathcal{L} = -\sum_{(u, v) \in \mathcal{E}} \log \sigma(\mathbf{z}_u^\top \mathbf{z}_v) - \sum_{(u', v') \in \mathcal{E}^-} \log(1 - \sigma(\mathbf{z}_{u'}^\top \mathbf{z}_{v'}))$$
- **Optimization:** Adam optimizer ($\text{lr} = 0.01$, $\text{weight\_decay} = 10^{-4}$), 100 epochs, early stopping on validation loss.
- **Evaluation Metrics:**
  - **ROC-AUC:** Area Under Receiver Operating Characteristic Curve ($> 0.82$)
  - **Average Precision (AP):** Precision-Recall Area Under Curve ($> 0.80$)
  - **Precision, Recall, F1:** Evaluated at 0.5 decision threshold on validation set.

---

## 7. Model Governance & Auditability

1. **Explicit Provenance:** When an investigator confirms a model lead, the generated relationship record is tagged with provenance `MODEL_CONFIRMED`, storing the model version, raw score, and reviewer user identity.
2. **Dismissal Integrity:** Dismissing a lead sets the lead status to `DISMISSED` with the analyst's rationalization notes. Zero modifications are made to the entity graph.
3. **System Logs:** All inference executions, feature computations, and human reviews are logged in PostgreSQL `AuditLogs` and `GraphAnalysisRuns`.
