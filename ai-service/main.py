import sys
import os

# Ensure smartgym_ai module is on python path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from smartgym_ai.main import app

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("smartgym_ai.main:app", host="0.0.0.0", port=8000, reload=True)
