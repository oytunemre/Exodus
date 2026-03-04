import config

ADMIN = {
    "email": config.ADMIN_EMAIL,
    "password": config.ADMIN_PASSWORD,
    "role": "Admin",
    "firstName": "Admin",
    "lastName": "User",
}

SELLER = {
    "email": config.SELLER_EMAIL,
    "password": config.SELLER_PASSWORD,
    "role": "Seller",
    "firstName": "Mehmet",
    "lastName": "Yılmaz",
    "businessName": "Yılmaz Elektronik",
    "phoneNumber": "+905551234567",
}

CUSTOMER = {
    "email": config.CUSTOMER_EMAIL,
    "password": config.CUSTOMER_PASSWORD,
    "role": "Customer",
    "firstName": "Ahmet",
    "lastName": "Kaya",
    "phoneNumber": "+905559876543",
}

PRODUCTS = [
    {
        "name": "Sony WH-1000XM5 Bluetooth Kulaklık",
        "description": "Endüstri lideri gürültü engelleme özelliğine sahip premium kulaklık",
        "price": 4299.99,
        "stock": 50,
        "brand": "Sony",
        "sku": "SONY-WH1000XM5-BLK",
    },
    {
        "name": "Samsung Galaxy S24 Ultra 256GB",
        "description": "S Pen destekli, 200MP kameralı amiral gemisi akıllı telefon",
        "price": 42999.0,
        "stock": 30,
        "brand": "Samsung",
        "sku": "SAMS-S24U-256-TIT",
    },
    {
        "name": "MacBook Pro 14 M3 Chip",
        "description": "Apple M3 işlemcili, 18 saat pil ömürlü profesyonel dizüstü bilgisayar",
        "price": 74999.0,
        "stock": 20,
        "brand": "Apple",
        "sku": "APPL-MBP14-M3-512",
    },
]

CATEGORIES = [
    {"name": "Elektronik", "description": "Elektronik ürünler ve aksesuarlar"},
    {"name": "Giyim", "description": "Erkek, kadın ve çocuk giyim ürünleri"},
    {"name": "Ev & Yaşam", "description": "Ev dekorasyonu ve yaşam ürünleri"},
]

ADDRESSES = [
    {
        "title": "Ev",
        "firstName": "Ahmet",
        "lastName": "Kaya",
        "phone": "+905559876543",
        "city": "İstanbul",
        "district": "Kadıköy",
        "neighborhood": "Moda",
        "street": "Moda Caddesi",
        "buildingNo": "15",
        "apartmentNo": "3",
        "zipCode": "34710",
        "isDefault": True,
    },
    {
        "title": "İş",
        "firstName": "Ahmet",
        "lastName": "Kaya",
        "phone": "+905559876543",
        "city": "İstanbul",
        "district": "Beşiktaş",
        "neighborhood": "Levent",
        "street": "Büyükdere Caddesi",
        "buildingNo": "100",
        "apartmentNo": "5",
        "zipCode": "34394",
        "isDefault": False,
    },
]

COUPONS = [
    {"code": "EXODUS10", "discountPercent": 10},
    {"code": "YENI20", "discountPercent": 20},
]

REVIEWS = [
    {
        "rating": 5,
        "comment": "Harika bir ürün, kesinlikle tavsiye ederim. Hızlı kargo ve sağlam paketleme.",
    },
    {
        "rating": 4,
        "comment": "Ürün beklentilerimi karşıladı. Kalite fiyat dengesi iyi.",
    },
]
