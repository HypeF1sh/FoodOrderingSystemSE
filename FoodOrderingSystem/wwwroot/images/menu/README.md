# Menu photos (seed data)

Starter photos for the 15 seeded dishes. Committed to git so every PC shows them.
`DbSeeder` (step 6) links each one to its dish by name, but only when the dish has no photo yet.

- Resized to 900 px wide, JPEG quality 82 (about 60 KB each).
- Photos the admin uploads later go to `wwwroot/uploads/menu/` instead, which is git-ignored.
- Adding a seeded dish photo: put `dish-name.jpg` here and add one line to `SeedDishPhotos` in `Data/DbSeeder.cs`.

Source: supplied by Jordan, 6 Nov 2026. If they are AI-generated, say so in the report's image credits
and AI-use declaration.
