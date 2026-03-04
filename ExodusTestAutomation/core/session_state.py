"""
Session state — test çalışması boyunca oluşturulan ID'leri ve token'ları saklar.
Payload factory bu değerleri kullanarak tutarlı request'ler oluşturur.
"""

session = {
    # Tokens
    "admin_token": None,
    "seller_token": None,
    "customer_token": None,
    # Created resource IDs
    "created_category_id": None,
    "created_brand_id": None,
    "created_product_id": None,
    "created_listing_id": None,
    "created_cart_id": None,
    "created_cart_item_id": None,
    "created_order_id": None,
    "created_address_id": None,
    "created_review_id": None,
    "created_campaign_id": None,
    "created_coupon_id": None,
    "created_wishlist_item_id": None,
    # Seller/admin specific
    "seller_id": None,
    "customer_id": None,
}


def reset():
    """Session'ı sıfırla (cleanup veya yeni çalıştırma için)."""
    for key in session:
        session[key] = None


def get(key: str):
    return session.get(key)


def set(key: str, value):
    session[key] = value
