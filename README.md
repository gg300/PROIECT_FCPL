STRUCTURE:
MetroREX/                          
│
├── MetroREX.csproj
├── Program.cs
├── Theme.cs
│
├── assets/
│   └── logo.png
│
├── Controls/                      <- controale desenate manual 
│   ├── HeroPanel.cs               (conține și LinePanel)
│   ├── LogoBox.cs
│   ├── StationButton.cs
│   └── StationLine.cs
│
├── Forms/                         <- ferestrele aplicației
│   ├── MainForm.cs                
│   ├── LoginForm.cs               (pasul 4)
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
├── Models/                        <- PASUL 1 (acesta) 
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
├── Data/                          <- pasul 2: citire/scriere JSON și Excel
│   ├── JsonStore.cs
│   └── ExcelProductStore.cs
│
├── Services/                      <- pasul 3: logica magazinului
│   ├── Clock.cs                   
│   ├── AuthService.cs
│   ├── ShopService.cs
│   ├── NotificationService.cs
│   ├── CouponService.cs
│   ├── EmailService.cs
│   └── Session.cs                 (utilizatorul curent + coșul)
│
└── data/                          
    ├── products.xlsx
    ├── users.json
    └── coupons.txt
