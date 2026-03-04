import os
from pathlib import Path
from dotenv import load_dotenv

load_dotenv(Path(__file__).parent / ".env")

BASE_URL = os.getenv("BASE_URL", "http://localhost:5013")
TIMEOUT = int(os.getenv("TIMEOUT", "30"))
SWAGGER_URL = f"{BASE_URL}/swagger/v1/swagger.json"

ADMIN_EMAIL = os.getenv("ADMIN_EMAIL", "admin@exodus.test")
ADMIN_PASSWORD = os.getenv("ADMIN_PASSWORD", "Admin1234!")

SELLER_EMAIL = os.getenv("SELLER_EMAIL", "satici@exodus.test")
SELLER_PASSWORD = os.getenv("SELLER_PASSWORD", "Satici1234!")

CUSTOMER_EMAIL = os.getenv("CUSTOMER_EMAIL", "musteri@exodus.test")
CUSTOMER_PASSWORD = os.getenv("CUSTOMER_PASSWORD", "Musteri1234!")
