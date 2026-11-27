# OxyGenAI – To-do

_Update: 27 November 2026._

## Tim

| Member | Nama | Pegang |
| :--- | :--- | :--- |
| 1 | **Satrio** | UI (desain) + backend |
| 2 | **Deva** | Front end |
| 3 | **Brian** | Front end |
| 4 | **Rere** | Backend |

## Aturan

1. Sebelum mulai, pastikan tugas yang ditunggu **sudah selesai dan jalan dengan baik**. Kalau tidak, hubungi PIC-nya atau buat issue baru dan assign ke dia.
2. Sebelum memindahkan issue atau menandai selesai, **pastikan dulu benar selesai, lalu kabari semua**.
3. Selesai artinya: syarat di kolom "Selesai kalau" terpenuhi, `dotnet build` dan `dotnet test AIEnvironmentalTracker.slnx` lolos, dan sudah dicoba di app.
4. Front end mulai setelah desain halamannya disetujui.
5. Kerja di branch sendiri, merge ke `main` lewat PR yang direview satu orang.
6. Yang mengubah, yang menulis dokumentasinya.
7. Jangan commit password, API key, atau connection string.
8. Ada hambatan? Kabari di hari yang sama.

## Daftar tugas

Dikerjakan dari atas ke bawah. Kolom **Tunggu No** berisi nomor yang harus selesai dulu (beserta yang ditunggu oleh nomor itu). Kalau tertulis **Tidak ada**, bisa langsung dikerjakan.

**Status:** `Belum` · `Jalan` · `Cek` (sudah dibuat, tinggal diperiksa) · `Nunggu key` · `Selesai`

| No | Tugas | PIC | Tunggu No | Selesai kalau | Status |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | Desain dasar: warna, font, navbar, login, register, usage tracker | Satrio | Tidak ada | Disetujui tim | Cek |
| 2 | Tanya dosen: apakah app WinForms + WebView2 memenuhi syarat | Satrio | Tidak ada | Jawaban dibagikan ke tim | Belum |
| 3 | Putuskan cara kelas diagram dipasangkan dengan Identity (tipe ID, password) | Rere | Tidak ada | Keputusan dicatat di docs dan disetujui | Belum |
| 4 | Tes Gemini dengan API key asli, tes ulang model yang tadinya error 503 | Rere | Tidak ada | Balasan asli tersimpan; model yang stabil ditambahkan | Nunggu key |
| 5 | Cek fitur deteksi aplikasi AI (`scanDeviceActivity`) di PC asli | Satrio | Tidak ada | Dicoba dengan app AI terbuka dan tertutup; batasannya dicatat | Belum |
| 6 | Desain sisanya: landing, error page, dashboard, compare, admin, jendela Windows, tombol deteksi | Satrio | 1 | Semua kondisi tergambar (normal, kosong, loading, error) | Jalan |
| 7 | Periksa hasil redesign: tema dan navbar | Deva | 1 | Sesuai desain; menu sesuai role; halaman aktif ditandai | Cek |
| 8 | Rapikan login dan register | Deva | 1 | Link "Forgot password?" dihapus; login dan register tetap jalan | Jalan |
| 9 | Periksa hasil redesign: usage tracker dan Ask Gemini | Brian | 1 | Sesuai desain; input salah ada pesannya; data tersimpan | Cek |
| 10 | Muat user dari database menjadi objek `RegisteredUser` / `Admin` | Rere | 3 | Tanpa password plain text; role dan suspend benar; ada test | Belum |
| 11 | Landing page (tamu lihat landing di `/`, user login masuk ke tracker) | Deva | 6 | Ada penjelasan fitur, cara estimasi, tombol Register dan Login | Belum |
| 12 | Halaman akun dan error page | Deva | 6, 8 | Hanya menu yang berfungsi yang tampil; error page punya tombol kembali | Belum |
| 13 | Tampilan admin panel | Deva | 6 | Suspend dan ubah role minta konfirmasi; pesan penolakan tampil | Belum |
| 14 | Dashboard dengan grafik | Brian | 6 | Grafik harian, mingguan, bulanan; angkanya sama dengan tabel | Belum |
| 15 | Tampilan Compare models | Brian | 6 | Terbaca di jendela kecil; hasil terbaik ditandai; model gagal terlihat | Belum |
| 16 | Jendela Windows: ikon, layar loading, pesan error | Brian | 6 | Sesuai desain; ikon tampil di `.exe` dan taskbar | Belum |
| 17 | Pakai `RegisteredUser`: catat usage, laporan, rekomendasi, bandingkan model | Rere | 10 | App memakai method kelas itu; bug daftar model kosong diperbaiki; test lolos | Belum |
| 18 | Pakai `Admin`: kelola, tambah, edit, suspend user; dokumentasikan login/register | Rere | 10 | Admin panel lewat kelas itu; aturan admin tetap berlaku; test lolos | Belum |
| 19 | Service deteksi aplikasi AI (nama app dan durasi) | Satrio | 5, 17 | Hanya baca nama app; "tidak terdeteksi" ditangani; ada test | Belum |
| 20 | Cek manual: landing, login, akun, admin | Brian | 11, 12, 13, 18 | Dicoba sebagai tamu, user, dan admin; bug jadi issue | Belum |
| 21 | Cek manual: black box test 23, Ask Gemini, Compare | Rere | 4, 9, 15 | Balasan asli dengan jumlah token; screenshot diambil | Nunggu key |
| 22 | Tombol "Detect AI app" di usage tracker | Brian | 6, 19 | Nama model terisi otomatis dan masih bisa diubah | Belum |
| 23 | Catatan singkat service untuk front end (method, input, hasil, error) | Rere | 17, 18, 19 | Ditulis di `docs/` | Belum |
| 24 | Cek manual: jendela Windows, tracker, dashboard | Deva | 14, 16, 22 | Dicoba semua, termasuk menutup jendela; bug jadi issue | Belum |
| 25 | Cek tampilan semua halaman dan coba paket di PC tanpa .NET | Satrio | 7, 8, 9, 11–16, 22 | Beda dari desain dicatat; setup dengan Neon berhasil | Belum |
| 26 | Merge final ke `main` dan buat paket `.zip` | Rere | 1–25 | Build dan test lolos; `publish.ps1` dari `main`; zip dibagikan | Belum |
| 27 | Update worksheet dan dokumen, masing-masing bagiannya | Semua | 26 | Isi cocok dengan aplikasi | Belum |
| 28 | Latihan demo, masing-masing menjelaskan bagiannya | Semua | 27 | Satu kali latihan penuh tanpa error | Belum |

## Opsional (kerjakan kalau disetujui)

| Tugas | PIC | Selesai kalau |
| :--- | :--- | :--- |
| User yang di-suspend langsung keluar (sekarang bisa sampai 30 menit) | Satrio | Akses hilang dalam sekitar 1 menit |
| Log file untuk app yang sudah diinstal | Satrio | Log tersimpan tanpa data rahasia |

## Sudah selesai

- Register, login, role admin, admin panel, akun admin bawaan `admin@email.com`
- Usage tracker, rekomendasi, data tren harian/mingguan/bulanan
- Rumus energi, karbon, dan air (sumber ada di kode)
- Ask Gemini dengan pilihan model, Compare models, batas pemakaian
- Database Neon, setup paket khusus Neon
- Aplikasi Windows (WinForms + WebView2), script paket, 104 test otomatis

## Tidak dikerjakan

Deploy ke web · database lokal · email, export CSV, audit log · batas percobaan login · angka energi berbeda per model Gemini · provider AI lain · black box test baru untuk fitur setelah Modul 10
