# LibBooks

A web application for managing a book library, built with **F# and ASP.NET Core MVC**, with **MySQL/MariaDB** for data storage. Users can browse, add, edit, and delete books, create accounts, and manage their own books.

## Project status

The project is under development and testing and is not production-ready. Account passwords are currently stored as plain text; **password encryption and hashing have not been implemented**. Sign-in uses a temporary session, and SMTP settings must be configured to enable email account activation.

## Run

Requirements: **.NET 10 SDK** and a **MySQL or MariaDB** server.

1. Start the database server. By default, the app connects to `localhost:3306` as `root` and creates the `DBLibBooksFSharp` database and its tables at startup. The database user must have permission to create them. Configure `LIBBOOKS_DB_SERVER`, `LIBBOOKS_DB_PORT`, `LIBBOOKS_DB_USER`, and `LIBBOOKS_DB_PASSWORD` as environment variables if needed.
2. In PowerShell, set the database password if required, then start the app:

   ```powershell
   $env:LIBBOOKS_DB_PASSWORD = "<database password>"
   dotnet restore
   dotnet run --launch-profile LibBooksFSharp
   ```

   If your database does not require a password, omit the environment-variable line.
3. Open `https://localhost:7190` or `http://localhost:5190`. Configure valid SMTP settings in `appsettings.json` to use email account activation.

Do not store real passwords in project files or the repository; use environment variables or a suitable secrets store.

---

# LibBooks

Eine Webanwendung zur Verwaltung einer Buchbibliothek, entwickelt mit **F# und ASP.NET Core MVC**. **MySQL/MariaDB** dient zur Speicherung der Daten. Bücher können angezeigt, hinzugefügt, bearbeitet und gelöscht werden. Benutzer können Konten erstellen und ihre eigenen Bücher verwalten.

## Projektstatus

Das Projekt befindet sich in Entwicklung und Erprobung und ist nicht für den produktiven Einsatz bereit. Kontopasswörter werden derzeit im Klartext gespeichert; **eine Verschlüsselung oder ein Hashing der Passwörter wurde nicht implementiert**. Die Anmeldung verwendet eine temporäre Sitzung. Für die Aktivierung von Konten per E-Mail müssen SMTP-Einstellungen konfiguriert werden.

## Ausführen

Voraussetzungen: **.NET 10 SDK** und ein **MySQL- oder MariaDB-Server**.

1. Datenbankserver starten. Standardmäßig verbindet sich die Anwendung mit `localhost:3306` als `root` und erstellt beim Start die Datenbank `DBLibBooksFSharp` samt Tabellen. Der Datenbankbenutzer benötigt die entsprechenden Erstellungsrechte. Bei Bedarf können `LIBBOOKS_DB_SERVER`, `LIBBOOKS_DB_PORT`, `LIBBOOKS_DB_USER` und `LIBBOOKS_DB_PASSWORD` als Umgebungsvariablen gesetzt werden.
2. In PowerShell bei Bedarf das Datenbankpasswort setzen und anschließend die Anwendung starten:

   ```powershell
   $env:LIBBOOKS_DB_PASSWORD = "<Datenbankpasswort>"
   dotnet restore
   dotnet run --launch-profile LibBooksFSharp
   ```

   Ist kein Datenbankpasswort erforderlich, die Zeile zum Setzen der Umgebungsvariable weglassen.
3. `https://localhost:7190` oder `http://localhost:5190` öffnen. Für die E-Mail-Aktivierung gültige SMTP-Einstellungen in `appsettings.json` konfigurieren.

Echte Passwörter nicht in Projektdateien oder im Repository speichern. Stattdessen Umgebungsvariablen oder einen geeigneten Secrets-Speicher verwenden.

---

# مكتبة الكتب

تطبيق ويب لإدارة مكتبة كتب، مبني باستخدام **F# وASP.NET Core MVC**، مع **MySQL/MariaDB** لتخزين البيانات. يتيح استعراض الكتب وإضافتها وتعديلها وحذفها، وإنشاء حسابات المستخدمين وإدارة الكتب الخاصة بكل مستخدم.

## حالة المشروع

المشروع في مرحلة التطوير والاختبار، وليس جاهزًا للاستخدام الإنتاجي. كلمات مرور الحسابات مخزّنة حاليًا كنص عادي؛ **لم يُنفّذ تشفيرها أو تجزئتها**. كما أن تسجيل الدخول يعتمد على جلسة مؤقتة، وإعدادات البريد الإلكتروني تحتاج إلى تهيئة قبل استخدام تفعيل الحسابات.

## التشغيل

المتطلبات: **.NET 10 SDK** وخادم **MySQL أو MariaDB**.

1. شغّل خادم قاعدة البيانات. افتراضيًا يستخدم التطبيق `localhost:3306` والمستخدم `root`، ويُنشئ قاعدة `DBLibBooksFSharp` والجداول عند بدء التشغيل. يجب أن يملك مستخدم قاعدة البيانات صلاحية الإنشاء. يمكن ضبط `LIBBOOKS_DB_SERVER` و`LIBBOOKS_DB_PORT` و`LIBBOOKS_DB_USER` و`LIBBOOKS_DB_PASSWORD` كمتغيرات بيئة.
2. في PowerShell، عيّن كلمة مرور قاعدة البيانات عند الحاجة، ثم شغّل التطبيق:

   ```powershell
   $env:LIBBOOKS_DB_PASSWORD = "<كلمة مرور قاعدة البيانات>"
   dotnet restore
   dotnet run --launch-profile LibBooksFSharp
   ```

   إذا لم تتطلب قاعدة البيانات كلمة مرور، احذف سطر تعيين المتغير.
3. افتح `https://localhost:7190` أو `http://localhost:5190`. أضف إعدادات SMTP صالحة في `appsettings.json` لاستخدام تفعيل الحسابات عبر البريد.

لا تضع كلمات مرور حقيقية في ملفات المشروع أو المستودع؛ استخدم متغيرات البيئة أو مخزن أسرار مناسبًا.
