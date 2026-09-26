"""
Graph dataset conversion utilities.
Transforms raw node/edge JSON structures into PyTorch and PyTorch Geometric Data representations.
"""
from typing import List, Dict, Any, Tuple, Optional
import numpy as np
from .features import extract_node_features, FEATURE_DIM

try:
    import torch
    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False

try:
    from torch_geometric.data import Data as PyGData
    PYG_AVAILABLE = True
except ImportError:
    PYG_AVAILABLE = False


def build_graph_tensors(
    nodes: List[Dict[str, Any]], 
    edges: List[Dict[str, Any]]
) -> Tuple[Any, Any, Dict[str, int], Dict[int, str]]:
    """
    Builds (x, edge_index, id_to_idx, idx_to_id) as PyTorch tensors or numpy arrays.
    """
    feat_matrix, id_to_idx, idx_to_id = extract_node_features(nodes, edges)

    # Build bidirectional edge list
    src_indices = []
    dst_indices = []

    for edge in edges:
        s_id = edge.get("source") or edge.get("sourceEntityId") or edge.get("Source")
        t_id = edge.get("target") or edge.get("targetEntityId") or edge.get("Target")
        if s_id in id_to_idx and t_id in id_to_idx:
            u = id_to_idx[s_id]
            v = id_to_idx[t_id]
            if u != v: # Prevent self-loops in primary edge index
                src_indices.extend([u, v])
                dst_indices.extend([v, u])

    if TORCH_AVAILABLE:
        x_tensor = torch.tensor(feat_matrix, dtype=torch.float32)
        if len(src_indices) > 0:
            edge_index = torch.tensor([src_indices, dst_indices], dtype=torch.long)
        else:
            edge_index = torch.empty((2, 0), dtype=torch.long)
        return x_tensor, edge_index, id_to_idx, idx_to_id
    else:
        edge_index_np = np.array([src_indices, dst_indices], dtype=np.int64) if src_indices else np.empty((2, 0), dtype=np.int64)
        return feat_matrix, edge_index_np, id_to_idx, idx_to_id


def build_pyg_data(nodes: List[Dict[str, Any]], edges: List[Dict[str, Any]]) -> Optional[Any]:
    """
    Constructs a torch_geometric.data.Data object if PyTorch Geometric is available.
    """
    if not TORCH_AVAILABLE or not PYG_AVAILABLE:
        return None

    x, edge_index, id_to_idx, _ = build_graph_tensors(nodes, edges)
    return PyGData(x=x, edge_index=edge_index)
