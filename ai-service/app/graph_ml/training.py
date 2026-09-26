"""
Link prediction self-supervised training for Graph Attention Networks.
Trains representation learning on synthetic graph datasets using negative sampling and BCE loss.
"""
from typing import Dict, Any, Tuple
import random

try:
    import torch
    import torch.nn as nn
    import torch.optim as optim
    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False

from .gat_model import GATNet


def sample_negative_edges(
    num_nodes: int, 
    pos_edge_index: Any, 
    num_neg_samples: int
) -> Any:
    """
    Samples non-existent edges (u, v) for negative contrastive training.
    """
    if not TORCH_AVAILABLE:
        return []

    existing = set()
    for i in range(pos_edge_index.size(1)):
        u = int(pos_edge_index[0, i])
        v = int(pos_edge_index[1, i])
        existing.add((u, v))
        existing.add((v, u))

    neg_edges = []
    attempts = 0
    max_attempts = num_neg_samples * 10

    while len(neg_edges) < num_neg_samples and attempts < max_attempts:
        attempts += 1
        u = random.randint(0, num_nodes - 1)
        v = random.randint(0, num_nodes - 1)
        if u != v and (u, v) not in existing:
            neg_edges.append((u, v))
            existing.add((u, v))

    if len(neg_edges) == 0:
        return torch.empty((2, 0), dtype=torch.long)

    srcs = [e[0] for e in neg_edges]
    dsts = [e[1] for e in neg_edges]
    return torch.tensor([srcs, dsts], dtype=torch.long)


def train_gat_link_prediction(
    model: GATNet,
    x: Any,
    edge_index: Any,
    epochs: int = 10,
    lr: float = 0.01
) -> Dict[str, Any]:
    """
    Executes self-supervised link prediction training loop.
    """
    if not TORCH_AVAILABLE:
        return {"epochs": 0, "final_loss": 0.0, "status": "torch_unavailable"}

    optimizer = optim.Adam(model.parameters(), lr=lr, weight_decay=1e-4)
    criterion = nn.BCEWithLogitsLoss()

    model.train()
    loss_history = []

    for epoch in range(epochs):
        optimizer.zero_grad()
        embeddings, _ = model(x, edge_index)

        # Positive pairs: existing edges
        if edge_index.size(1) == 0:
            break

        pos_u = embeddings[edge_index[0]]
        pos_v = embeddings[edge_index[1]]
        pos_scores = (pos_u * pos_v).sum(dim=-1)

        # Negative pairs: sampled non-edges
        neg_edge_index = sample_negative_edges(x.size(0), edge_index, edge_index.size(1))
        if neg_edge_index.size(1) > 0:
            neg_u = embeddings[neg_edge_index[0]]
            neg_v = embeddings[neg_edge_index[1]]
            neg_scores = (neg_u * neg_v).sum(dim=-1)

            scores = torch.cat([pos_scores, neg_scores])
            labels = torch.cat([torch.ones_like(pos_scores), torch.zeros_like(neg_scores)])
        else:
            scores = pos_scores
            labels = torch.ones_like(pos_scores)

        loss = criterion(scores, labels)
        loss.backward()
        optimizer.step()
        loss_history.append(float(loss.item()))

    model.eval()
    return {
        "epochs": len(loss_history),
        "final_loss": loss_history[-1] if loss_history else 0.0,
        "loss_history": loss_history
    }
