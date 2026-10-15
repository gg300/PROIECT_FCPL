# MetroREX – magazin online de haine (Windows Forms, .NET 8)

Proiectul 5 – Online site management. Echipa: Timotei si George.

## Cum se ruleaza

1. Instaleaza **.NET 8 SDK** (verificare: `dotnet --list-sdks`). Pe Linux poti folosi `dotnet-install.sh`.
2. Deschide `MetroREX/MetroREX.csproj` in Rider sau Visual Studio si apasa Run.
   Din terminal: `cd MetroREX` si apoi `dotnet run` (sau `./run.sh`).
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

## Functionalitati suplimentare (in afara cerintei)

| # | Functionalitate | Unde |
|---|---|---|
| 1 | **Abonamente la vanzatori**: 10% reducere la produsele vanzatorilor la care esti abonat (max. 3) | `SubscriptionService`, `SubscriptionsView` |
| 2 | **T-Rex Points (loialitate)**: 50 de puncte bonus la crearea contului, 1 punct / 10 lei platiti la livrare, niveluri Bronz / Argint (x1.25) / Aur (x1.5), folosire la checkout (1 punct = 0,10 lei, max. 50% din suma) | `LoyaltyService`, `LoyaltyView`, `CheckoutView` |
| 3 | **Retururi (RMA)**: in 14 zile de la livrare; Cerere → Aprobat (a doua zi) → Finalizat (a treia zi): stocul revine in Excel, rambursare pe metoda de plata sau in puncte (+10%), punctele castigate pe partea returnata se anuleaza | `ReturnService`, `ReturnsView`, `NewReturnView` |

Contul demo are 322 de puncte (nivel Argint) si o comanda livrata care poate fi returnata.

## Fisiere de date

| Fisier | Continut |
|---|---|
| `Storage/products.xlsx` | foaia **Products** (produse, preturi, stoc, culori, rating) si foaia **Specs** (specificatii: ProductId / Name / Value) |
| `Storage/users.json` | conturi, profil, wish list, cupoane, comenzi, notificari |
| `Storage/coupons.txt` | cupoanele generate zilnic, pentru magazin (`COD|categorie|expirare|procent`) |
| `Storage/email.json` | setarile SMTP reale (nu se pune in git); model: `email.sample.json` |
| `Storage/state.json` | ultima zi in care au rulat procesele zilnice (se creeaza la rulare) |

## Arhitectura pe scurt

Aplicatia are straturi clare, iar dependentele merg intr-o singura directie:

```text
Forms / Controls  (UI)  →  Services (logica)  →  Data (citire/scriere)  →  Storage (fisiere)
                                  ↑
                               Models (structurile de date folosite de toate straturile)
```

- **Forms** nu au logica de business: apeleaza serviciile si afiseaza rezultatul.
- **Services** contin regulile magazinului (preturi, cupoane, comenzi, retururi, puncte).
- **Data** stie doar sa citeasca si sa scrie fisiere (JSON, Excel, txt), fara reguli de magazin.

## Structura proiectului

```text
MetroREX/
├── Program.cs              punctul de intrare: porneste aplicatia
├── Theme.cs                culorile, fonturile si stilul comun al aplicatiei
├── Sfx.cs                  sunetele (pornit/oprit cu Ctrl+M)
├── MetroREX.csproj         configuratia proiectului (.NET 8, pachetul ClosedXML)
├── run.sh                  script de rulare pentru terminal
├── dotnet-install.sh       script de instalare a .NET SDK
├── fixstupidshit.sh        script auxiliar de reparare/curatare (uz intern)
├── assets/                 logo si sunete
├── mock_data/              date de test / de exemplu pentru generarea starii initiale
├── bin/, obj/, out/        rezultatul build-ului (generate automat, nu se editeaza)
│
├── Controls/               controale desenate manual
│   ├── AccessCard            card pentru optiunile de acces (login / cont nou / fara cont)
│   ├── CategoryTile          placa unei categorii de produse
│   ├── HeroPanel             panoul mare din antet
│   ├── LogoBox, Wordmark     logo-ul si numele „MetroREX”
│   ├── RoundedButton         buton cu colturi rotunjite
│   ├── StarRating            afisarea/alegerea stelelor 1–5
│   ├── StationButton         buton in stilul de „statie de metrou”
│   ├── StationLine           linia decorativa de metrou
│   └── Surface               suprafata/cardul de baza pe care se aseaza restul
│
├── Data/                   citire/scriere fisiere
│   ├── AppPaths              caile catre fisierele din Storage
│   ├── AtomicFile            scriere sigura (fisier temporar, apoi inlocuire)
│   ├── IStore                interfata comuna pentru stocarea datelor
│   ├── JsonStore             citire/scriere users.json si state.json
│   ├── ExcelProductStore     citire/scriere products.xlsx (ClosedXML)
│   └── CouponFileStore       citire/scriere coupons.txt
│
├── Models/                 structurile de date
│   ├── AppState              starea aplicatiei (ultima zi procesata etc.)
│   ├── User, UserProfile     cont si date de profil
│   ├── Product               produs
│   ├── Cart, CartItem        cosul de cumparaturi
│   ├── Order, OrderStatus    comanda si starile ei
│   ├── PaymentMethod, CardDetails   metoda de plata si datele cardului
│   ├── Coupon                cupon de reducere
│   ├── Loyalty               puncte si nivel de loialitate
│   ├── Notification          notificare pentru utilizator
│   └── ReturnRequest         cerere de retur
│
├── Services/               logica magazinului
│   ├── ShopServices          grupeaza toate serviciile (un singur punct de acces)
│   ├── ShopRepository        accesul comun la date pentru servicii
│   ├── ShopRuntime           procesele asincrone: zi noua + sincronizare Excel la 10 s
│   ├── Clock                 ceasul aplicatiei (poate fi schimbat pentru testare)
│   ├── Session               utilizatorul curent
│   ├── AuthService           creare cont si login
│   ├── PasswordHasher        hash-ul parolelor
│   ├── Validation            reguli de validare a campurilor
│   ├── ErrorMessages         textele erorilor
│   ├── Result                rezultat „reusit / eroare” returnat de servicii
│   ├── Money                 calcule si formatare pentru sume
│   ├── CatalogService        listarea produselor si categoriilor
│   ├── PriceSort             sortarea dupa pret
│   ├── PricingService        variatia zilnica ±20% din pretul de baza
│   ├── ProductSyncService    sincronizarea cu products.xlsx
│   ├── InventoryService      stoc si „produs din nou pe stoc”
│   ├── CartService           operatii pe cos
│   ├── WishListService       wish list
│   ├── CouponService         cupon zilnic AAA-BBB-CCC, stergerea celor expirate
│   ├── SubscriptionService   abonamente la vanzatori
│   ├── LoyaltyService        T-Rex Points
│   ├── CheckoutRequest       datele trimise la finalizarea comenzii
│   ├── OrderQuote            calculul totalului (reduceri, puncte)
│   ├── CheckoutService       cupon, profil vizitator, plata, e-mail
│   ├── CheckoutOutcome       rezultatul finalizarii comenzii
│   ├── OrderService          livrari, istoric, evaluari 1–5
│   ├── ReturnService         retururi (RMA)
│   ├── NotificationService   notificari
│   └── Email/
│       ├── IEmailSender          interfata de trimitere e-mail
│       ├── SmtpEmailSender       trimiterea prin SMTP
│       ├── SmtpSettings          citirea setarilor din email.json
│       └── OrderConfirmationEmail  continutul e-mailului de confirmare
│
├── Forms/                  ferestrele si ecranele
│   ├── MainForm              Login / Creare cont / Fara cont
│   ├── AuthFormBase          baza comuna pentru LoginForm si RegisterForm
│   ├── LoginForm, RegisterForm
│   ├── ShopForm              fereastra magazinului (antet, navigare, meniul „Contul meu”)
│   ├── AccountMenuStyle      stilul meniului „Contul meu”
│   ├── ViewBase              baza comuna a tuturor ecranelor din magazin
│   ├── CategoriesView, ProductListView (sortare dupa pret), ProductView (detalii + specificatii)
│   ├── CartView, CheckoutView
│   ├── ActiveOrdersView, OrderHistoryView, RatingDialog
│   ├── WishListView, CouponsView, NotificationsView, SubscriptionsView, SettingsView
│   ├── LoyaltyView           puncte si nivel
│   ├── ReturnsView, NewReturnView   lista retururilor si cererea de retur noua
│   ├── ProfileFields, Inputs   campuri de formular reutilizabile
│   ├── Ui, Symbols           ajutoare de desenare si simboluri
│   └── DebugClockDialog      metoda speciala de schimbare a datei
│
└── Storage/                datele initiale (vezi „Fisiere de date”)
```
