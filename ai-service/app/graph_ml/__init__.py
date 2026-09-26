"""
Graph Attention Network (GAT) and topological graph analytics module.
"""
from .features import extract_node_features, FEATURE_DIM
from .dataset import build_graph_tensors, build_pyg_data
from .gat_model import GATNet, PurePyTorchGATConv
from .inference import run_gat_inference, get_gat_model
from .training import train_gat_link_prediction, sample_negative_edges
from .evaluation import evaluate_link_prediction

__all__ = [
    "extract_node_features",
    "FEATURE_DIM",
    "build_graph_tensors",
    "build_pyg_data",
    "GATNet",
    "PurePyTorchGATConv",
    "run_gat_inference",
    "get_gat_model",
    "train_gat_link_prediction",
    "sample_negative_edges",
    "evaluate_link_prediction"
]
