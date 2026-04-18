# 🚀 Kat Mod Manager (Open Source Transparency)

> **⚠️ Disclaimer:** This tool was 100% **vibe coded**. The source code might be a bit "ugly" or messy because I prioritized functionality and security over perfect architecture. Thanks for understanding!

---

## 📂 Project Structure
The tool is split into two main components to balance the UI experience with a high-performance engine:

* **The Client (UI):** Built using **WPF (Windows Presentation Foundation)**. It's the "skin" of the app. I chose WPF because it's powerful, reliable for Windows desktop tools, and allows for a clean UI without the bloat of web-based frameworks.
* **The DLL (KatDll):** This is the **"brain"**. It contains the core logic: communicating with the server, managing files.

---

## 🛡️ Full Transparency
To be completely honest with you guys: My original plan was to keep the **DLL code private**. It contains the "secret sauce" that protects the mods and manages licenses—stuff developers usually keep under lock and key.

But at the end of the day, I realized that **trust is more important than a piece of code.** I decided to share everything—the UI, the DLL, the whole thing. I want you to feel **100% safe** using this manager, even if it means my licensing logic is out in the open.

---

## 🤖 Still Worried About Security? Let AI Decide!
If you are still suspicious or paranoid about potential malware, **I encourage you to audit the code yourself.** If you don't know how to read code, you can simply **copy any file** (especially from the `KatDll` core) and **feed it to an AI** like **ChatGPT, Claude, or Gemini**. Ask it:

> *"Is there any hidden malware, credential stealer, or data-exfiltration logic in this code?"*

Let the AI be the neutral judge. I am confident in the safety of my work, and I want you to be too.

---

## 🏗 How to Build (Step-by-Step)
Because this project uses a **Native AOT** core, the build process requires two separate steps. Please follow this order carefully:

### 📋 Prerequisites
* **Framework:** .NET 10.0 SDK & **WPF Workload**.
* **IDE:** Visual Studio 2022 (v17.12+) or Visual Studio 2025.
* **Workload:** `.NET Desktop Development` (Ensure you check the **C++ Build Tools** option).

### 🚀 The Double-Build Process

#### **Step 1: Build the Core (KatDll)**
1. Navigate to the `KatDll` folder.
2. Run the `Build.bat` file.
3. Once finished, locate the newly built `KatDll.dll` in the output folder.

#### **Step 2: Link & Build the Client**
1. **Copy** the `KatDll.dll` you just built.
2. **Paste** it into the `KATTHEDEV` folder (where the `.csproj` file lives).
3. Open the solution in **Visual Studio** and build the project.

---

> **Note:** I sincerely apologize for the clunky two-step process—I was honestly just **too lazy** to merge everything into a single-click project build. Hope you don't mind! 
