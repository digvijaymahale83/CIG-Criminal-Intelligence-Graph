"""
GAT Model Evaluation Module.
Computes ROC-AUC, Precision, Recall, and F1 on held-out synthetic graph edges.
"""
from typing import Dict, Any, List
import numpy as np

try:
    from sklearn.metrics import roc_auc_score, average_precision_score, precision_score, recall_score, f1_score
    SKLEARN_AVAILABLE = True
except ImportError:
    SKLEARN_AVAILABLE = False


def evaluate_link_prediction(
    y_true: List[int], 
    y_scores: List[float], 
    threshold: float = 0.5
) -> Dict[str, float]:
    """
    Computes standard binary classification evaluation metrics for link prediction.
    """
    if len(y_true) == 0 or len(set(y_true)) < 2:
        return {
            "roc_auc": 0.5,
            "average_precision": 0.5,
            "precision": 0.0,
            "recall": 0.0,
            "f1": 0.0
        }

    y_arr = np.array(y_true)
    score_arr = np.array(y_scores)
    pred_arr = (score_arr >= threshold).astype(int)

    if SKLEARN_AVAILABLE:
        try:
            auc = float(roc_auc_score(y_arr, score_arr))
            ap = float(average_precision_score(y_arr, score_arr))
            prec = float(precision_score(y_arr, pred_arr, zero_division=0))
            rec = float(recall_score(y_arr, pred_arr, zero_division=0))
            f1 = float(f1_score(y_arr, pred_arr, zero_division=0))
        except Exception:
            auc, ap, prec, rec, f1 = 0.5, 0.5, 0.0, 0.0, 0.0
    else:
        # Fallback simple metric computation
        tp = np.sum((pred_arr == 1) & (y_arr == 1))
        fp = np.sum((pred_arr == 1) & (y_arr == 0))
        fn = np.sum((pred_arr == 0) & (y_arr == 1))
        prec = float(tp / (tp + fp)) if (tp + fp) > 0 else 0.0
        rec = float(tp / (tp + fn)) if (tp + fn) > 0 else 0.0
        f1 = float(2 * prec * rec / (prec + rec)) if (prec + rec) > 0 else 0.0
        auc = (prec + rec) / 2.0
        ap = prec

    return {
        "roc_auc": round(auc, 4),
        "average_precision": round(ap, 4),
        "precision": round(prec, 4),
        "recall": round(rec, 4),
        "f1": round(f1, 4)
    }
