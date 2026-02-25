from dataclasses import dataclass


@dataclass
class TrainModelCommand:
    force_retrain: bool = False
