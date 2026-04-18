# 🚀 Kat Mod Manager (Open Source Transparency)

> **⚠️ Disclaimer:** This tool was 100% **vibe coded**. The source code might be a bit "ugly" or messy because I prioritized functionality and security over perfect architecture. Thanks for understanding!

---

## 📂 Project Structure
The tool is split into two main components to balance the UI experience with a high-performance engine:

* **The Client (UI):** This is the user interface—the **"skin"** of the app. It handles everything you see on the screen and sends your commands to the engine.
* **The DLL (KatDll):** This is the **"brain"**. It contains the core logic: communicating with the server, managing files, and the anti-reseller security system (licensing).

---

## 🛡️ Full Transparency
To be completely honest with you guys: My original plan was to keep the **DLL code private**. It contains the "secret sauce" that protects the mods and manages licenses—stuff developers usually keep under lock and key.

But at the end of the day, I realized that **trust is more important than a piece of code.** I decided to share everything—the UI, the DLL, the whole thing. I want you to feel **100% safe** using this manager, even if it means my licensing logic is out in the open.

---

## 🏗 How to Build (Step-by-Step)
Because this project uses a **Native AOT** core, the build process requires two separate steps. Please follow this order carefully:

### 📋 Prerequisites
Before you start, make sure you have the following installed:
* **SDK:** .NET 10.0 SDK (Required for the latest AOT optimizations).
* **IDE:** Visual Studio 2022 (v17.12+) or Visual Studio 2025.
* **Workload:** `.NET Desktop Development` (Ensure you check the **C++ Build Tools** option, as it's required for Native AOT).

### 🚀 The Double-Build Process

#### **Step 1: Build the Core (KatDll)**
1.  Navigate to the `KatDll` folder.
2.  Run the `Build.bat` file.
    * *This script handles the complex Native AOT compilation for you.*
3.  Once finished, locate the newly built `KatDll.dll` in the output folder.

#### **Step 2: Link & Build the Client**
1.  **Copy** the `KatDll.dll` you just built.
2.  **Paste** it into the `KATTHEDEV` folder (where the `.csproj` file lives).
3.  Open the solution in **Visual Studio**.
4.  Build the **Mod Manager Client** project.

---

> **Note:** I sincerely apologize for the clunky two-step process—I was honestly just **too lazy** to merge everything into a single-click project build. Hope you don't mind!
