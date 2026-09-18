FROM python:3.11-slim

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
        default-jre-headless \
        curl \
        ffmpeg \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .

# PyTorch (CPU-only)
RUN pip install --no-cache-dir \
        torch --index-url https://download.pytorch.org/whl/cpu

# Python dependencies
RUN pip install --no-cache-dir -r requirements.txt

# numpy 1.x (pickle compatibility)
RUN pip install --no-cache-dir "numpy==1.26.4"

# spaCy German model
RUN python -m spacy download de_core_news_lg

# GermanBERT
RUN python -c "\
from transformers import pipeline; \
print('Downloading dbmdz/bert-base-german-cased ...'); \
pipeline('fill-mask', model='dbmdz/bert-base-german-cased', top_k=10, device=-1); \
print('bert-base-german-cased cached')" \
    || echo "BERT download failed — vocab detection will be disabled at runtime"

# Whisper
RUN python -c "\
import whisper, os; \
m = os.getenv('WHISPER_MODEL', 'base'); \
print(f'Downloading Whisper {m} ...'); \
whisper.load_model(m); \
print('Whisper cached')" \
    || echo "Whisper download failed — transcription will return 503"

COPY main.py .
COPY feature_extractor.py .
COPY transcription.py .
COPY vocab_detector.py .
COPY vocab_game.py .
COPY word_enricher.py .

RUN mkdir -p models
COPY models/cefr_model.pkl models/

EXPOSE 8001

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8001", "--timeout-keep-alive", "120"]
