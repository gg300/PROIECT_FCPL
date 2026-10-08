#  MetroREX Project Structure

```text
MetroREX/
│
├── MetroREX.csproj
├── Program.cs
├── Theme.cs
│
├── assets/
│   └── logo.png
│
├── Controls/                      # Controale desenate manual 
│   ├── HeroPanel.cs               # (conține și LinePanel)
│   ├── LogoBox.cs
│   ├── StationButton.cs
│   └── StationLine.cs
│
├── Forms/                         # Ferestrele aplicației
│   ├── MainForm.cs                
│   ├── LoginForm.cs               # PASUL 4
│   ├── RegisterForm.cs
│   ├── HomeForm.cs
│   ├── CategoriesForm.cs
│   ├── ProductListForm.cs
│   ├── ProductForm.cs
│   ├── CartForm.cs / CheckoutForm.cs
│   ├── ActiveOrdersForm.cs
│   ├── OrderHistoryForm.cs
│   ├── WishListForm.cs
│   ├── CouponsForm.cs
│   └── SettingsForm.cs
│
├── Models/                        # PASUL 1: Modele de date
│   ├── User.cs
│   ├── UserProfile.cs
│   ├── Product.cs                 
│   ├── Cart.cs
│   ├── CartItem.cs
│   ├── Order.cs                   
│   ├── OrderStatus.cs
│   ├── PaymentMethod.cs
│   ├── Coupon.cs
│   └── Notification.cs
│
├── Data/                          # PASUL 2: Citire/scriere fișiere
│   ├── JsonStore.cs
│   └── ExcelProductStore.cs
│
├── Services/                      # PASUL 3: Logica magazinului
│   ├── Clock.cs                   
│   ├── AuthService.cs
│   ├── ShopService.cs
│   ├── NotificationService.cs
│   ├── CouponService.cs
│   ├── EmailService.cs
│   └── Session.cs                 # (utilizatorul curent + coșul)
│
└── data/                          # Baza de date locală
    ├── products.xlsx
    ├── users.json
    └── coupons.txt
