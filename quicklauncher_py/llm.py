"""
Local LLM integration module for the Quick Launcher.

This module defines functions for loading and using a local large language model
to interpret user commands. To use it, download a suitable model (e.g. the Qwen2.5 -0.5B
multilingual model) from HuggingFace and load it via :func:`load_model`.
"""

from __future__ import annotations

from typing import Optional

try:
    # These imports will succeed only if transformers and a backend (e.g. torch) are installed.
    from transformers import AutoModelForCausalLM, AutoTokenizer, pipeline
except ImportError:
    AutoModelForCausalLM = None  # type: ignore
    AutoTokenizer = None  # type: ignore
    pipeline = None  # type: ignore

# Torch is optional; if available we will use CUDA when detected.
try:
    import torch  # type: ignore
except Exception:  # pragma: no cover - optional dependency
    torch = None  # type: ignore

# Global references to the model, tokenizer and generation pipeline.
_MODEL = None  # type: ignore
_TOKENIZER = None  # type: ignore
_PIPELINE = None  # type: ignore

def load_model(model_name: str) -> None:
    """
    Load a HuggingFace model, tokenizer and text generation pipeline into memory.

    This function should be called once at application startup if LLM functionality is desired.

    :param model_name: HuggingFace model identifier (e.g. 'Qwen/Qwen2.5-0.5B').
    """
    global _MODEL, _TOKENIZER, _PIPELINE
    if AutoModelForCausalLM is None or AutoTokenizer is None or pipeline is None:
        raise ImportError(
            "transformers is not installed. Please install it via pip: pip install transformers torch"
        )
    # Load tokenizer and model from HuggingFace; trust_remote_code allows custom code from the repo.
    _TOKENIZER = AutoTokenizer.from_pretrained(model_name, trust_remote_code=True)
    _MODEL = AutoModelForCausalLM.from_pretrained(model_name, trust_remote_code=True)
    # Initialize a text-generation pipeline. Prefer CUDA if available.
    device_id = 0 if (torch is not None and torch.cuda.is_available()) else -1
    _PIPELINE = pipeline("text-generation", model=_MODEL, tokenizer=_TOKENIZER, device=device_id)


def interpret_command(text: str) -> Optional[str]:
    """
    Interpret a natural language string using the loaded model.

    If no model is loaded, this function simply returns the input text unchanged.

    Replace this implementation with a more sophisticated one to generate
    suggestions or parse commands.

    :param text: User input string.
    :return: A string representing the model's interpretation, or None if no model is loaded.
    """
    global _MODEL, _TOKENIZER, _PIPELINE
    if _MODEL is None or _TOKENIZER is None or _PIPELINE is None:
        # No model is available; return a simple echo
        return text

    # Generate a continuation of the input text.
    try:
        results = _PIPELINE(text, max_new_tokens=32)
    except Exception:
        # If generation fails for some reason, fall back to echo
        return text

    generated_text = results[0]["generated_text"]
    # Remove the original prompt from the generated text if present
    if generated_text.startswith(text):
        generated_text = generated_text[len(text):].strip()
    return generated_text
