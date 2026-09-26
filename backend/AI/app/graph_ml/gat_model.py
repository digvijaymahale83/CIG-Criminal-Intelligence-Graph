"""
Graph Attention Network (GAT) PyTorch & PyTorch Geometric Architecture.
Implements multi-head self-attention over graph neighborhoods for investigative entity representation learning.
"""
from typing import Tuple, Optional, Dict, Any
import numpy as np

try:
    import torch
    import torch.nn as nn
    import torch.nn.functional as F
    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False
    nn = object # type: ignore

try:
    from torch_geometric.nn import GATConv as PyGGATConv
    PYG_AVAILABLE = True
except ImportError:
    PYG_AVAILABLE = False


class PurePyTorchGATConv(nn.Module if TORCH_AVAILABLE else object):
    """
    Standard multi-head Graph Attention layer implemented in pure PyTorch.
    Guarantees mathematical correctness without requiring compiled C++ PyG binaries.
    """
    def __init__(
        self, 
        in_features: int, 
        out_features: int, 
        heads: int = 4, 
        concat: bool = True, 
        dropout: float = 0.1, 
        negative_slope: float = 0.2
    ):
        super().__init__()
        self.in_features = in_features
        self.out_features = out_features
        self.heads = heads
        self.concat = concat
        self.dropout = dropout
        self.negative_slope = negative_slope

        self.lin = nn.Linear(in_features, heads * out_features, bias=False)
        self.att_src = nn.Parameter(torch.empty(1, heads, out_features))
        self.att_dst = nn.Parameter(torch.empty(1, heads, out_features))
        self.bias = nn.Parameter(torch.zeros(heads * out_features if concat else out_features))

        self.reset_parameters()

    def reset_parameters(self):
        nn.init.xavier_uniform_(self.lin.weight)
        nn.init.xavier_uniform_(self.att_src)
        nn.init.xavier_uniform_(self.att_dst)
        nn.init.zeros_(self.bias)

    def forward(self, x: torch.Tensor, edge_index: torch.Tensor) -> Tuple[torch.Tensor, Tuple[torch.Tensor, torch.Tensor]]:
        """
        x: [N, in_features]
        edge_index: [2, E]
        """
        N = x.size(0)
        if N == 0:
            out_dim = self.heads * self.out_features if self.concat else self.out_features
            return torch.zeros((0, out_dim), device=x.device), (edge_index, torch.zeros((edge_index.size(1), self.heads), device=x.device))

        # Linear projection: [N, heads, out_features]
        h = self.lin(x).view(N, self.heads, self.out_features)

        # Add self-loops so isolated nodes can update their representations
        self_loops = torch.arange(N, dtype=torch.long, device=edge_index.device).unsqueeze(0).repeat(2, 1)
        if edge_index.size(1) > 0:
            full_edge_index = torch.cat([edge_index, self_loops], dim=1)
        else:
            full_edge_index = self_loops

        src, dst = full_edge_index[0], full_edge_index[1]

        # Compute attention coefficients: [E_full, heads]
        alpha_src = (h[src] * self.att_src).sum(dim=-1)
        alpha_dst = (h[dst] * self.att_dst).sum(dim=-1)
        alpha = F.leaky_relu(alpha_src + alpha_dst, negative_slope=self.negative_slope)

        # Softmax over incoming neighbors for each destination node dst
        # Numerical stability via max subtraction per destination
        alpha_max = torch.zeros(N, self.heads, device=x.device).scatter_reduce(
            0, dst.unsqueeze(-1).expand(-1, self.heads), alpha, reduce="amax", include_self=False
        )
        alpha_exp = torch.exp(alpha - alpha_max[dst])
        alpha_sum = torch.zeros(N, self.heads, device=x.device).scatter_add(
            0, dst.unsqueeze(-1).expand(-1, self.heads), alpha_exp
        ) + 1e-12
        alpha_norm = alpha_exp / alpha_sum[dst]
        alpha_drop = F.dropout(alpha_norm, p=self.dropout, training=self.training)

        # Message aggregation: sum_{src in N(dst)} alpha_{src, dst} * h[src]
        weighted_msgs = h[src] * alpha_drop.unsqueeze(-1) # [E_full, heads, out_features]
        out = torch.zeros(N, self.heads, self.out_features, device=x.device)
        out.scatter_add_(0, dst.view(-1, 1, 1).expand(-1, self.heads, self.out_features), weighted_msgs)

        if self.concat:
            out = out.view(N, self.heads * self.out_features)
            out = out + self.bias
        else:
            out = out.mean(dim=1)
            out = out + self.bias

        return out, (full_edge_index, alpha_norm)


class GATNet(nn.Module if TORCH_AVAILABLE else object):
    """
    2-Layer Graph Attention Network.
    Produces L2-normalized node embeddings in R^(N x out_features).
    """
    def __init__(
        self, 
        in_features: int = 19, 
        hidden_dim: int = 32, 
        out_features: int = 64, 
        heads: int = 4, 
        dropout: float = 0.1,
        model_version: str = "GAT-v1.0.0"
    ):
        super().__init__()
        self.in_features = in_features
        self.hidden_dim = hidden_dim
        self.out_features = out_features
        self.heads = heads
        self.dropout = dropout
        self.model_version = model_version

        # Layer 1: Multi-head attention (concatenated)
        self.conv1 = PurePyTorchGATConv(
            in_features=in_features,
            out_features=hidden_dim,
            heads=heads,
            concat=True,
            dropout=dropout
        )

        # Layer 2: Output projection (averaged over heads)
        self.conv2 = PurePyTorchGATConv(
            in_features=hidden_dim * heads,
            out_features=out_features,
            heads=heads,
            concat=False,
            dropout=dropout
        )

    def forward(
        self, 
        x: torch.Tensor, 
        edge_index: torch.Tensor
    ) -> Tuple[torch.Tensor, Any]:
        """
        Computes node representations:
        x: [N, in_features]
        edge_index: [2, E]
        Returns: (embeddings [N, out_features], attention_info)
        """
        N = x.size(0)
        if N == 0:
            return torch.zeros((0, self.out_features), device=x.device), None

        # Layer 1
        h1, att1 = self.conv1(x, edge_index)
        h1 = F.elu(h1)
        h1 = F.dropout(h1, p=self.dropout, training=self.training)

        # Layer 2
        h2, att2 = self.conv2(h1, edge_index)

        # L2 normalization for stable cosine similarity / dot product scoring
        embeddings = F.normalize(h2, p=2, dim=-1)
        return embeddings, att2
