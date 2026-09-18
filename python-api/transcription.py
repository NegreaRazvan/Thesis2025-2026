"""Whisper speech-to-text endpoint."""

import tempfile
import os
import logging

from fastapi import APIRouter, File, UploadFile, HTTPException
from pydantic import BaseModel

logger = logging.getLogger(__name__)

router = APIRouter()

_whisper_model = None  # lazy-loaded on first request

def _get_model():
    global _whisper_model
    if _whisper_model is None:
        try:
            import whisper
            model_name = os.getenv("WHISPER_MODEL", "base")  # tiny/base/small/medium
            logger.info(f"Loading Whisper model: {model_name}")
            _whisper_model = whisper.load_model(model_name)
            logger.info("Whisper model loaded.")
        except ImportError:
            raise HTTPException(
                status_code=503,
                detail="Whisper is not installed on the ML service. "
                       "Run: pip install openai-whisper"
            )
    return _whisper_model


class TranscriptionResponse(BaseModel):
    text: str
    language: str
    duration_seconds: float


@router.post("/transcribe", response_model=TranscriptionResponse)
async def transcribe_audio(file: UploadFile = File(...)):
    """
    Accepts a raw audio file (webm, mp4, wav, mp3, ogg, …).
    Returns the transcribed text, detected language, and clip duration.

    The browser MediaRecorder API typically emits audio/webm; opus which
    Whisper handles natively via ffmpeg.
    """
    if not file.content_type or not file.content_type.startswith("audio"):
        # Also accept video/webm which is what Chrome sometimes reports
        if file.content_type not in ("video/webm", "application/octet-stream"):
            logger.warning(f"Unexpected content type: {file.content_type}")

    audio_bytes = await file.read()
    if len(audio_bytes) < 1024:
        raise HTTPException(status_code=422, detail="Audio file is too small or empty.")

    # Write to a temp file because Whisper needs a file path
    suffix = _extension_from_content_type(file.content_type or "audio/webm")
    with tempfile.NamedTemporaryFile(suffix=suffix, delete=False) as tmp:
        tmp.write(audio_bytes)
        tmp_path = tmp.name

    try:
        model = _get_model()
        logger.info(f"Transcribing {len(audio_bytes)/1024:.1f} KB audio…")
        result = model.transcribe(
            tmp_path,
            language="de",          # force German recognition
            task="transcribe",
            fp16=False,             # CPU inference; set True if CUDA available
        )
        text      = result["text"].strip()
        language  = result.get("language", "de")
        duration  = float(result.get("duration", 0.0))
        logger.info(f"Transcription done. chars={len(text)}, lang={language}")
        return TranscriptionResponse(text=text, language=language, duration_seconds=duration)
    except Exception as e:
        logger.error(f"Whisper transcription failed: {e}")
        raise HTTPException(status_code=500, detail=f"Transcription failed: {str(e)}")
    finally:
        try:
            os.unlink(tmp_path)
        except OSError:
            pass


def _extension_from_content_type(ct: str) -> str:
    mapping = {
        "audio/webm": ".webm",
        "video/webm": ".webm",
        "audio/mp4":  ".mp4",
        "audio/mpeg": ".mp3",
        "audio/ogg":  ".ogg",
        "audio/wav":  ".wav",
        "audio/x-wav": ".wav",
    }
    for k, v in mapping.items():
        if k in ct:
            return v
    return ".webm"