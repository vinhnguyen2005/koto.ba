Ok. Hôm nay là **Thứ 2**, deadline vẫn là **23:59 CN 8/3**.

Tức là các bạn có:

> 🕒 6 ngày (T2 → CN)
> Không còn lý do “không kịp”.

Tôi sẽ chỉnh lại thành **Kế hoạch 6 ngày – Full App – Không lùi bước**.

---

# 🚨 CHIẾN LƯỢC CỨU DỰ ÁN (GIỮ NGUYÊN)

## 1️⃣ Vertical Completion

Mỗi feature phải hoàn chỉnh:

UI → API → Service → DB → Realtime

Không để “backend xong nhưng UI chưa có”.

---

## 2️⃣ Freeze Architecture

* Không đổi interface
* Không thêm layer
* Không tái cấu trúc
* Không tranh luận

Ai phụ trách gì → giữ nguyên.

---

# 🎯 MỤC TIÊU CUỐI CÙNG (FULL APP)

Ứng dụng phải có đầy đủ:

## Core

* Auth
* Chat 1–1
* Chat nhóm
* Message persistence
* Paging
* Typing
* Presence

## Interaction

* Reaction
* Upload ảnh
* Upload PDF
* Preview

## Social

* Story (expire 24h)
* Current Thought

## AI

* Suggestion
* Multiple tone
* Không auto-send

---

# 📆 KẾ HOẠCH 6 NGÀY (T2 → CN)

---

# 🧱 THỨ 2 – FOUNDATION DAY

🎯 Mục tiêu: Auth + Chat 1–1 chạy realtime

---

## Dũng

[x] Identity hoàn chỉnh
[x] Register / Login chạy được
[x] User profile
[x] Migration xong
[x] Presence memory-based

## Nga

[] Conversation create direct
[] Get conversation list
[] SignalR ChatHub
[] Join group theo ConversationId
[] BroadcastMessage basic

## Vinh

[] Message entity
[] SendMessageAsync
[] Save DB
[] GetMessages (paging basic)

## Hoàn

[] UI Login
[] UI Chat page cơ bản
[] Connect SignalR
[] Hiển thị message realtime

---

👉 Kết thúc T2:
Chat 1–1 realtime hoạt động.

---

# 🧱 THỨ 3 – GROUP + TYPING + PRESENCE

🎯 Mục tiêu: Messaging hoàn chỉnh

---

## Nga

[] Typing event
[] BroadcastTyping
[] OnDisconnected xử lý đúng

## Vinh

[] CreateGroupConversation
[] Add participant
[] Paging hoàn chỉnh

## Dũng

[] Presence broadcast

## Hoàn

[] Conversation list UI
[] Group chat UI
[] Typing indicator UI
[] Presence indicator

---

👉 Kết thúc T3:
Chat nhóm + typing + online status hoạt động.

---

# 🧱 THỨ 4 – REACTION + ATTACHMENT

🎯 Mục tiêu: Interaction hoàn chỉnh

---

## Vinh

[] Reaction entity
[] AddOrUpdateReaction
[] RemoveReaction
[] GetReactions
[] Attachment entity
[] Upload local file
[] Save path DB

## Nga

[] BroadcastReaction
[] BroadcastAttachmentEvent

## Hoàn

[] UI reaction
[] Show reaction
[] Upload image
[] Preview image
[] Download PDF

---

👉 Kết thúc T4:
Reaction + Upload ảnh/PDF chạy được.

---

# 🧱 THỨ 5 – SOCIAL FEATURES

🎯 Mục tiêu: Story + Current Thought

---

## Vinh

[] Story entity
[] CurrentThought entity
[] ExpiresAt logic (query filter là đủ)

## Dũng

[] Worker cleanup (nếu kịp)
[] Hoặc đơn giản filter khi query

## Hoàn

[] Story UI
[] Current Thought UI
[] Auto hide expired story

---

👉 Kết thúc T5:
Story + Thought hoạt động.

---

# 🧱 THỨ 6 – AI INTEGRATION

🎯 Mục tiêu: AI suggestion chạy thật

---

## Dũng

[] IAIReplyService
[] Call AI API
[] Tone enum
[] Return 3 suggestions

## Hoàn

[] Nút “Suggest reply”
[] Chọn tone
[] Hiển thị suggestion
[] Copy vào input (không auto send)

---

👉 Kết thúc T6:
AI suggestion hoạt động.

---

# 🧱 CHỦ NHẬT – LOCKDOWN + FIX

---

## Buổi sáng

Full flow test:

[] Register
[] Login
[] Create group
[] Chat
[] Typing
[] Reaction
[] Upload ảnh
[] Upload PDF
[] Story
[] Thought
[] AI suggestion

---

## Buổi chiều

Fix crash bug

KHÔNG:

* chỉnh UI
* thêm feature
* refactor

---

## 20:00 – 22:00

Final test 2 account
Demo rehearsal

---

# 🧨 LUẬT BẮT BUỘC

* Merge mỗi tối 22:30
* Không đổi DTO
* Không đổi interface
* Không tự ý tối ưu
* Không làm song song 2 feature

---

# ⚠️ PHÂN TÍCH THỰC TẾ

Với 6 ngày:

Khả năng hoàn thành:

* Nếu kỷ luật cao: 70–80%
* Nếu vẫn tranh luận kiến trúc: 30%

Điểm nguy hiểm nhất:

1. SignalR bug
2. Merge conflict
3. AI API lỗi
4. Upload file sai path

Phải khóa 4 điểm này.
