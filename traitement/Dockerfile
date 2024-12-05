FROM python:latest

COPY app.py  .

COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir -r requirements.txt*

EXPOSE 5000

CMD ["python","app.py"]  