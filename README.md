# 💰 Budget Tracker

A C# console application that helps users manage their personal finances. 
It supports multiple users, allows tracking income and expenses, enforces category-based budget limits, and provides undo/redo functionality for transactions. 
All data is stored locally in JSON files, making it simple and persistent.

---

## 📝 Features

### User Management
- Multi-user system with login and logout.
- Register new users with username and password.
- Track transactions per user.

Transaction Management
- Add income and expense transactions with description, category, and amount.
- Edit or delete transactions.
- Undo and redo recent transactions.

### Budget Monitoring
- Monthly budget limits per category (e.g., Groceries, Rent, Entertainment).
- Automatic warnings when spending exceeds category limits.

### Reportings
- View current balance.
- Display complete transaction history.

### Reportings
- All data is saved in JSON files: `users.json`, `budget.json`, and `limits.json`.
