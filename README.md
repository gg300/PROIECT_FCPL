# MetroREX – magazin online de haine (Windows Forms, .NET 8)

Proiectul 5 – Online site management. Echipa: Timotei si George.

## Cum se ruleaza

1. Instaleaza **.NET 8 SDK** (verificare: `dotnet --list-sdks`).
2. Deschide `MetroREX/MetroREX.csproj` in Rider sau Visual Studio si apasa Run.
   Din terminal: `cd MetroREX` si apoi `dotnet run`.
3. La primul build se descarca pachetul **ClosedXML** (citirea/scrierea Excel), deci e nevoie de internet.

Datele de lucru se copiaza la build in `bin/.../Storage/` si acolo sunt modificate de aplicatie
(fisierele din `MetroREX/Storage/` raman ca stare initiala).

## Cont demo

| Utilizator | Parola |
|---|---|
| `demo` | `Demo1234` |

Are deja: 3 produse in wish list, o comanda livrata (cu produse de evaluat) si o comanda activa.

## Taste pentru testare (in magazin)

| Taste | Ce face |
|---|---|
| **Ctrl+Shift+T** | fereastra de testare: seteaza orice data/ora, +1 ora, +1 zi, +3 zile, ceasul real |
| Ctrl+Shift+D | +1 zi |
| Ctrl+Shift+R | inapoi la ceasul real |
| Ctrl+M | sunet pornit / oprit |

## E-mailul de confirmare

Copiaza `Storage/email.sample.json` in `Storage/email.json` si completeaza datele SMTP
(pentru Gmail: o *App Password* din contul Google, nu parola obisnuita). Fara acest fisier
comanda se plaseaza normal, dar aplicatia anunta ca e-mailul nu a putut fi trimis.

## Fisiere de date

| Fisier | Continut |
|---|---|
| `Storage/products.xlsx` | foaia **Products** (produse, preturi, stoc, culori, rating) si foaia **Specs** (specificatii: ProductId / Name / Value) |
| `Storage/users.json` | conturi, profil, wish list, cupoane, comenzi, notificari |
| `Storage/coupons.txt` | cupoanele generate zilnic, pentru magazin (`COD|categorie|expirare|procent`) |
| `Storage/state.json` | ultima zi in care au rulat procesele zilnice |

## Structura

```text
MetroREX/
├── Program.cs, Theme.cs, Sfx.cs
├── assets/                 logo si sunete
├── Controls/               controale desenate manual (butoane, carduri, antet)
├── Data/                   citire/scriere JSON, Excel (ClosedXML), coupons.txt
├── Models/                 User, Product, Order, Coupon, Notification ...
├── Services/               logica magazinului
│   ├── ShopRuntime.cs        procesele asincrone: zi noua + sincronizare Excel la 10 s
│   ├── Clock.cs              ceasul aplicatiei (poate fi schimbat pentru testare)
│   ├── PricingService.cs     variatia zilnica ±20% din pretul de baza
│   ├── InventoryService.cs   „produs din nou pe stoc”
│   ├── CouponService.cs      cupon zilnic AAA-BBB-CCC, stergerea celor expirate
│   ├── OrderService.cs       livrari, istoric, evaluari 1–5
│   └── CheckoutService.cs    cupon, profil vizitator, plata, e-mail
└── Forms/                  ferestrele si ecranele
    ├── MainForm               Login / Creare cont / Fara cont
    ├── LoginForm, RegisterForm
    ├── ShopForm               fereastra magazinului (antet, navigare, meniul „Contul meu”)
    ├── CategoriesView, ProductListView (sortare dupa pret), ProductView (detalii + specificatii)
    ├── CartView, CheckoutView
    ├── ActiveOrdersView, OrderHistoryView, RatingDialog
    ├── WishListView, CouponsView, NotificationsView, SubscriptionsView, SettingsView
    └── DebugClockDialog       metoda speciala de schimbare a datei
```
