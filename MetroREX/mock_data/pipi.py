import json
import random
import base64

def generate_random_base64(length):
    """Generates a random base64 string to simulate hashes and salts."""
    return base64.b64encode(bytes((random.getrandbits(8) for _ in range(length)))).decode('utf-8')

def generate_mock_users(count=300):
    first_names = ["Gigel", "Ion", "Maria", "Elena", "Andrei", "Alexandru", "Mihai", "Ana", "Florin", "Diana"]
    last_names = ["Popescu", "Ionescu", "Radu", "Dumitrescu", "Stan", "Stoica", "Gheorghe", "Matei", "Ciobanu"]
    streets = ["Lamatan rozeta", "Bulevardul Unirii", "Calea Victoriei", "Strada Primaverii", "Aleea Trandafirilor"]
    
    users = []
    
    for _ in range(count):
        first = random.choice(first_names)
        last = random.choice(last_names)
        
        name = f"{first} {last}"
        username = f"{first.lower()}{last.lower()}{random.randint(1, 9999)}"
        email = f"{username}@gmail.com"
        address = f"{random.choice(streets)} {random.randint(1, 150)}"
        phone = f"07{random.randint(10000000, 99999999)}"
        
        # Simulating the PasswordHash and PasswordSalt from users.json
        password_hash = generate_random_base64(32) 
        password_salt = generate_random_base64(16)
        
        user_obj = {
            "Username": username,
            "PasswordHash": password_hash,
            "PasswordSalt": password_salt,
            "Profile": {
                "Name": name,
                "Address": address,
                "Email": email,
                "Phone": phone
            },
            "WishList": [],
            "Subscriptions": ["ReptileThreads"] if random.random() > 0.5 else [],
            "Coupons": [],
            "Orders": [],
            "Notifications": []
        }
        users.append(user_obj)
        
    return users

# Generate 300 users and save to file
mock_data = generate_mock_users(300)

with open("mock_users.json", "w") as f:
    json.dump(mock_data, f, indent=2)

print("Successfully generated 300 mock users in 'mock_users.json'")
