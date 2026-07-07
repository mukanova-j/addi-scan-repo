# AddiScan

AddiScan reads a food label photograph and reports which additives carry documented health risks. A user uploads an image. Tesseract.NET extracts the ingredient text. The backend matches the detected additives against a curated risk database. The grading logic scores each match against seven criteria. The interface returns a plain language report. Registered users can save their scan history.

The application supports food literacy and informed consumer choice. Packaged labels list additives by technical chemical names that most shoppers cannot interpret. AddiScan closes that gap at the point of purchase.
 is a capstone project for MSIT 5910 at the University of the People. It runs on an eight week solo development timeline and targets a working browser based prototype rather than a production release.

## Features

1. Image upload with OCR text extraction using Tesseract.NET.
2. Additive detection that matches extracted text against the curated database.
3. Risk scoring through a seven criteria grading system.
4. A results interface that explains each flagged additive in plain language.
5. User authentication that lets a registered user save scan history.
6. A manual text entry fallback when OCR cannot read a label.

## Technology

AddiScan runs the backend and frontend on ASP.NET Core (.NET). Data lives in SQL Server or SQLite, so a solo developer can run the project without server setup. The OCR stage uses the open source Tesseract.NET engine. Passwords hash and salt with bcrypt. Git and GitHub handle version control. ProjectLibre tracks the schedule and the critical path.

## The grading system

Each additive receives a score from 0 to 5 and a named risk band. The system stays permissive by default, so only proven, significant risks raise a score. Suspicion or theoretical harm alone does not count. Carcinogenicity scores follow IARC classifications. Ban scores follow documented regulatory decisions.

### The seven criteria

1. Carcinogenicity, maximum 4 points, anchored to the IARC group of the additive.
2. Ban status, maximum 3 points, counting outright bans in sovereign countries or major regulatory blocs.
3. Allergic and sensitivity reactions, maximum 2 points, separating reactions in sensitive individuals from reactions in the general population.
4. Cumulative and long term exposure risk, maximum 2 points, judged against real dietary patterns and ADI exceedances.
5. Origin, maximum 1 point, where fully synthetic sources score higher than natural or biologically derived ones.
6. Effect on children and vulnerable groups, maximum 2 points, for harm beyond the general adult population.
7. Functional necessity, a modifier from minus 1 to plus 1. This is the only criterion that can lower a score. A critical safety function, such as preventing botulism, earns minus 1. A purely cosmetic role earns plus 1.

### The formula

The raw total sums all seven criteria. The final score maps that total onto a 0 to 5 scale.

```
raw_points  = sum of all seven criteria
final_score = clamp(raw_points, 0, 15) / 15 * 5
```

Round the final score to one decimal place. Always present the raw total alongside the final score, for example 3.0 out of 5 with a raw total of 9 out of 15.

### Risk bands

1. 0.0 to 1.0, Safe. No significant concern at typical dietary exposure.
2. 1.1 to 2.0, Low risk. Minor concerns, generally safe at normal intake.
3. 2.1 to 3.0, Moderate. Some evidence of harm, caution advised for sensitive groups or high consumers.
4. 3.1 to 4.0, High risk. Significant evidence, consider avoiding, especially for children.
5. 4.1 to 5.0, Avoid. Strong evidence of serious harm, banned in multiple jurisdictions.

Every graded output shows the per criterion breakdown, the raw total, the final score, and the band. The system never returns a bare number.

## The dataset

The repository ships a base list of roughly 377 additives, drawn from the EFSA authorised additives list. Each row holds an E number and a common name. The scoring pipeline enriches each entry with the seven criteria scores and a provenance record.

## How a scan works

The user uploads a photograph over HTTPS. The backend validates the file type and size before any processing begins, so malformed input never reaches the OCR engine. Tesseract.NET extracts the ingredient text. The matcher compares the text against the additive database. The grading logic scores each matched additive. The results interface lists the flagged additives, their scores, their risk bands, and a plain language explanation of the concern. If a registered user chooses to save the scan, the application writes a minimal record scoped to that account.

## Security and privacy

AddiScan builds these controls into the architecture rather than adding them after the code runs.

1. The application hashes and salts passwords with bcrypt and never stores plain text credentials.
2. HTTPS protects all traffic between the frontend and the backend API.
3. The backend validates uploaded file type and size before OCR processing.
4. Role based permissions restrict database access, so an authenticated request reads only its own records.
5. Data minimization limits stored scan history to the fields the save feature needs.
6. A privacy notice at account creation states what the application stores and how a user deletes an account.
7. Scanning without an account stays available, so a user who declines consent still receives the core analysis.

## Provenance and fairness

An outdated classification could mislead a user, so AddiScan records the source and the review date of every classification and displays that provenance beside each flagged result. The seven criteria scoring keeps the reasoning inspectable rather than hidden inside one number. The prompt log lets a reviewer audit how the dataset came together.

## Scope

This version excludes native mobile apps, barcode scanning, nutritional macro analysis, and multi language support. It assumes label photographs arrive in legible condition and that the OCR engine handles standard printed text rather than handwritten or stylized fonts.

## Data sources

1. IARC Monographs for carcinogenicity classification, https://monographs.iarc.who.int/
2. EFSA food additives for EU safety opinions and ADI values, https://www.efsa.europa.eu/en/topics/topic/food-additives
3. JECFA (WHO and FAO) for international ADI values, https://www.who.int/groups/joint-fao-who-expert-committee-on-food-additives
4. Open Food Facts for product presence data, https://world.openfoodfacts.org/
5. FDA CFR Title 21 for US approval and ban status, https://www.ecfr.gov/current/title-21