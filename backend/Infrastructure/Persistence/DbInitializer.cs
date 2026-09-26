using Domain.Entities;
using Infrastructure.Security;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            if (await context.Users.AnyAsync())
            {
                return; // Database has already been seeded
            }

            logger.LogInformation("Seeding authentic synthetic Maharashtra Police investigative universe...");

            var hashedPassword = PasswordHasher.HashPassword("Maharashtra@2024");

            // 1. Users
            var dcpSharma = new User
            {
                Id = "usr-dcp-sharma",
                Email = "dcp.sharma@mahapolice.gov.in",
                PasswordHash = hashedPassword,
                FullName = "DCP Rajesh Sharma",
                Role = "INVESTIGATOR",
                BadgeNumber = "MH-POL-04821",
                Agency = "Maharashtra Police",
                Rank = "Deputy Commissioner of Police",
                Unit = "Crime Branch CID",
                CreatedAtUtc = DateTime.UtcNow
            };

            var adminUser = new User
            {
                Id = "usr-admin-deshmukh",
                Email = "admin@mahapolice.gov.in",
                PasswordHash = hashedPassword,
                FullName = "Special IG Ashok Deshmukh",
                Role = "ADMIN",
                BadgeNumber = "MH-POL-00109",
                Agency = "Maharashtra Police",
                Rank = "Inspector General",
                Unit = "State Intelligence Dept",
                CreatedAtUtc = DateTime.UtcNow
            };

            var analystPatil = new User
            {
                Id = "usr-analyst-patil",
                Email = "analyst.patil@mahapolice.gov.in",
                PasswordHash = hashedPassword,
                FullName = "Analyst Neha Patil",
                Role = "ANALYST",
                BadgeNumber = "MH-POL-12094",
                Agency = "Maharashtra Police",
                Rank = "Senior Intelligence Analyst",
                Unit = "State Cyber Cell",
                CreatedAtUtc = DateTime.UtcNow
            };

            context.Users.AddRange(dcpSharma, adminUser, analystPatil);

            // 2. Fictional Cases
            // Case 1: Primary Investigation (matches frontend default inv-2026-0841)
            var case1 = new Case
            {
                Id = "inv-2026-0841",
                CaseNumber = "CASE-2026-0841",
                Title = "Operation Iron Vault Syndicate",
                Description = "Investigation into illegal remittance routing and cellular mule terminals operating across Pune and Western Maharashtra.",
                Classification = "RESTRICTED",
                Priority = "Critical",
                Status = "Active",
                Category = "Cyber Fraud & Hawala",
                Jurisdiction = "Pune Police Commissionerate",
                District = "Pune",
                FirNumber = "FIR-104/2026",
                PoliceStation = "Shivajinagar Police Station",
                LeadOfficerId = dcpSharma.Id,
                LeadOfficerName = dcpSharma.FullName,
                EvidenceCount = 2,
                EntityCount = 5,
                CreatedBy = dcpSharma.FullName,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-15),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            var casePune = new Case
            {
                Id = "inv-2026-001",
                CaseNumber = "CASE-2026-001",
                Title = "Operation Pune Hawala & Cyber Network",
                Description = "Investigation into illegal remittance routing and cellular mule terminals operating across Pune and Western Maharashtra.",
                Classification = "RESTRICTED",
                Priority = "Critical",
                Status = "Active",
                Category = "Cyber Fraud & Hawala",
                Jurisdiction = "Pune Police Commissionerate",
                District = "Pune",
                FirNumber = "FIR-104/2026-P",
                PoliceStation = "Shivajinagar Police Station",
                LeadOfficerId = dcpSharma.Id,
                LeadOfficerName = dcpSharma.FullName,
                EvidenceCount = 0,
                EntityCount = 0,
                CreatedBy = dcpSharma.FullName,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-15),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            // Case 2: Mumbai Port Interception (Shares planted phone +91-9000001001 and vehicle MH12AB4321)
            var case2 = new Case
            {
                Id = "inv-2026-002",
                CaseNumber = "CASE-2026-002",
                Title = "Operation Mumbai Port Cargo Interception",
                Description = "Customs diversion and unmanifested logistics syndicates operating through maritime docks and transit corridors.",
                Classification = "RESTRICTED",
                Priority = "High",
                Status = "Active",
                Category = "Maritime & Smuggling",
                Jurisdiction = "Mumbai Crime Branch (Port Zone)",
                District = "Mumbai City",
                FirNumber = "FIR-088/2026",
                PoliceStation = "Yellow Gate Police Station",
                LeadOfficerId = analystPatil.Id,
                LeadOfficerName = analystPatil.FullName,
                EvidenceCount = 1,
                EntityCount = 4,
                CreatedBy = analystPatil.FullName,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
            };

            var case3 = new Case
            {
                Id = "case-2026-003",
                CaseNumber = "CASE-2026-003",
                Title = "Operation Nashik Corridor (Synthetic Demo Data)",
                Description = "Synthetic demonstration dataset verifying false-positive safeguards. Transporter network operating in Nashik rural corridor.",
                Classification = "RESTRICTED",
                Priority = "Low",
                Status = "Active",
                Category = "Logistics",
                Jurisdiction = "Maharashtra",
                District = "Nashik",
                PoliceStation = "Panchavati PS",
                FirNumber = "FIR-2026-NSK-102",
                LeadOfficerId = analystPatil.Id,
                LeadOfficerName = analystPatil.FullName,
                EvidenceCount = 0,
                EntityCount = 1,
                CreatedBy = analystPatil.FullName,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            context.Cases.AddRange(case1, casePune, case2, case3);
            await context.SaveChangesAsync();

            // 3. Evidence Items (with authentic raw byte SHA-256 hashes matching disk files)
            var ev1 = new Evidence
            {
                Id = "ev-2026-001-cdr",
                CaseId = case1.Id,
                FileName = "call_records_pune.csv",
                MimeType = "text/csv",
                FileSize = 369,
                StoragePath = "2026-08/call_records_pune.csv",
                Sha256Hash = "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
                UploadedById = dcpSharma.Id,
                UploadedByName = dcpSharma.FullName,
                UploadedAtUtc = DateTime.UtcNow.AddDays(-14),
                ProcessingStatus = "EXTRACTED",
                Clearance = "RESTRICTED",
                Description = "CDR logs tracking communication between target cellular units in Pune Central.",
                Version = 1
            };

            var ev2 = new Evidence
            {
                Id = "ev-2026-001-surv",
                CaseId = case1.Id,
                FileName = "surveillance_report_pune.txt",
                MimeType = "text/plain",
                FileSize = 683,
                StoragePath = "2026-08/surveillance_report_pune.txt",
                Sha256Hash = "7c2c73dfdc485cbf788c2ed0f27e6be2a0859a9024ea3723aaf3cc18f6369168",
                UploadedById = dcpSharma.Id,
                UploadedByName = dcpSharma.FullName,
                UploadedAtUtc = DateTime.UtcNow.AddDays(-12),
                ProcessingStatus = "REVIEW_REQUIRED",
                Clearance = "RESTRICTED",
                Description = "Field surveillance log documenting vehicle movement and suspect rendezvous.",
                Version = 1
            };

            var ev3 = new Evidence
            {
                Id = "ev-2026-002-port",
                CaseId = case2.Id,
                FileName = "mumbai_port_interception.txt",
                MimeType = "text/plain",
                FileSize = 539,
                StoragePath = "2026-08/mumbai_port_interception.txt",
                Sha256Hash = "519694cf560cc64ca9b8ffbc8856b94950ccf4000f62b05244bcd79019944d45",
                UploadedById = analystPatil.Id,
                UploadedByName = analystPatil.FullName,
                UploadedAtUtc = DateTime.UtcNow.AddDays(-9),
                ProcessingStatus = "REVIEW_REQUIRED",
                Clearance = "RESTRICTED",
                Description = "Consignment interception memo at Mumbai Dock Area referencing cross-case logistics vehicle.",
                Version = 1
            };

            context.EvidenceItems.AddRange(ev1, ev2, ev3);

            // 4. Extraction Job for ev-2026-001-surv
            var job1 = new ExtractionJob
            {
                Id = "job-20260814-001",
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                Status = "COMPLETED",
                RawTextSnippet = "Subject Rahul Mehta was observed arriving at commercial complex in Shivajinagar. Rahul Mehta used vehicle MH12AB4321...",
                TotalEntitiesFound = 6,
                TotalRelationshipsFound = 3,
                TotalEventsFound = 1,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12),
                StartedAtUtc = DateTime.UtcNow.AddDays(-12).AddSeconds(2),
                CompletedAtUtc = DateTime.UtcNow.AddDays(-12).AddSeconds(5),
                CreatedById = dcpSharma.Id,
                CreatedByName = dcpSharma.FullName
            };

            var jobMum = new ExtractionJob
            {
                Id = "job-mum-001",
                EvidenceId = ev3.Id,
                CaseId = case2.Id,
                Status = "COMPLETED",
                RawTextSnippet = "Cellular tower intercept logged for terminal +91-9000001001 near Shivajinagar Docks sector.",
                TotalEntitiesFound = 3,
                TotalRelationshipsFound = 1,
                TotalEventsFound = 1,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-9),
                StartedAtUtc = DateTime.UtcNow.AddDays(-9).AddSeconds(2),
                CompletedAtUtc = DateTime.UtcNow.AddDays(-9).AddSeconds(5),
                CreatedById = analystPatil.Id,
                CreatedByName = analystPatil.FullName
            };
            context.ExtractionJobs.AddRange(job1, jobMum);
            await context.SaveChangesAsync();

            // 5. Staged Extracted Entities (Pending Review)
            var stagedEnt1 = new ExtractedEntity
            {
                Id = "ext-ent-001",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EntityType = "PERSON",
                RawValue = "Rahul Mehta",
                NormalizedValue = "Rahul Mehta",
                Confidence = 0.96,
                SourceLocation = "Line 1",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var stagedEnt2 = new ExtractedEntity
            {
                Id = "ext-ent-002",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EntityType = "VEHICLE",
                RawValue = "MH12AB4321",
                NormalizedValue = "MH12AB4321",
                Confidence = 0.94,
                SourceLocation = "Line 2",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var stagedEnt3 = new ExtractedEntity
            {
                Id = "ext-ent-003",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EntityType = "PHONE",
                RawValue = "+91-9000001001",
                NormalizedValue = "+919000001001",
                Confidence = 0.98,
                SourceLocation = "Line 3",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var stagedEnt4 = new ExtractedEntity
            {
                Id = "ext-ent-004",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EntityType = "PERSON",
                RawValue = "Sameer Patil",
                NormalizedValue = "Sameer Patil",
                Confidence = 0.93,
                SourceLocation = "Line 4",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var stagedEnt5 = new ExtractedEntity
            {
                Id = "ext-ent-005",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EntityType = "LOCATION",
                RawValue = "Pune Central",
                NormalizedValue = "Pune Central",
                Confidence = 0.91,
                SourceLocation = "Line 4",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var stagedEnt6 = new ExtractedEntity
            {
                Id = "ext-ent-006",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EntityType = "ORGANIZATION",
                RawValue = "Apex Trading Syndicate",
                NormalizedValue = "APEX TRADING SYNDICATE",
                Confidence = 0.88,
                SourceLocation = "Line 5",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            context.ExtractedEntities.AddRange(stagedEnt1, stagedEnt2, stagedEnt3, stagedEnt4, stagedEnt5, stagedEnt6);

            // 6. Staged Extracted Relationships (Pending Review)
            var stagedRel1 = new ExtractedRelationship
            {
                Id = "ext-rel-001",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                SourceExtractedEntityId = stagedEnt1.Id,
                TargetExtractedEntityId = stagedEnt2.Id,
                SourceNormalizedValue = "Rahul Mehta",
                TargetNormalizedValue = "MH12AB4321",
                RelationshipType = "USED",
                Confidence = 0.94,
                SourceLocation = "Line 2: \"Rahul Mehta used vehicle MH12AB4321\"",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var stagedRel2 = new ExtractedRelationship
            {
                Id = "ext-rel-002",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                SourceExtractedEntityId = stagedEnt1.Id,
                TargetExtractedEntityId = stagedEnt4.Id,
                SourceNormalizedValue = "Rahul Mehta",
                TargetNormalizedValue = "Sameer Patil",
                RelationshipType = "MET",
                Confidence = 0.91,
                SourceLocation = "Line 4: \"Rahul Mehta met with Sameer Patil near Pune Central\"",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var stagedRel3 = new ExtractedRelationship
            {
                Id = "ext-rel-003",
                ExtractionJobId = job1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                SourceExtractedEntityId = stagedEnt1.Id,
                TargetExtractedEntityId = stagedEnt3.Id,
                SourceNormalizedValue = "Rahul Mehta",
                TargetNormalizedValue = "+919000001001",
                RelationshipType = "CALLED",
                Confidence = 0.96,
                SourceLocation = "Line 3: \"Subject was observed making multiple phone calls using number +91-9000001001\"",
                ReviewStatus = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            context.ExtractedRelationships.AddRange(stagedRel1, stagedRel2, stagedRel3);
            await context.SaveChangesAsync();

            // 7. Canonical Verified Entities (Human Approved)
            var canEnt1 = new EntityItem
            {
                Id = "can-ent-pune-001",
                CaseId = case1.Id,
                Type = "PERSON",
                CanonicalName = "Rahul Mehta",
                NormalizedValue = "Rahul Mehta",
                VerificationStatus = "VERIFIED",
                Confidence = 0.98,
                RiskLevel = "HIGH",
                Location = "Shivajinagar, Pune",
                District = "Pune",
                PhoneNumber = "+91-9000001001",
                VehicleNumber = "MH12AB4321",
                AccountNumber = "ACC-9021-PUNE-01",
                AliasesJson = "[\"R. Sharma\", \"Rahul Sharma\"]",
                Description = "Identified as primary financial logistics coordinator for the Western Maharashtra Hawala nexus.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEnt2 = new EntityItem
            {
                Id = "can-ent-pune-002",
                CaseId = case1.Id,
                Type = "PHONE",
                CanonicalName = "+91-9000001001",
                NormalizedValue = "+919000001001",
                PhoneNumber = "+91-9000001001",
                VerificationStatus = "VERIFIED",
                Confidence = 0.99,
                RiskLevel = "CRITICAL",
                Description = "Burner SIM card used to coordinate illicit transactions and cargo dispatch.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEnt3 = new EntityItem
            {
                Id = "can-ent-pune-003",
                CaseId = case1.Id,
                Type = "VEHICLE",
                CanonicalName = "MH12AB4321",
                NormalizedValue = "MH12AB4321",
                VehicleNumber = "MH12AB4321",
                VerificationStatus = "VERIFIED",
                Confidence = 0.95,
                RiskLevel = "HIGH",
                Description = "Dark blue sedan intercepted during Pune-Mumbai transit.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEnt4 = new EntityItem
            {
                Id = "can-ent-pune-004",
                CaseId = case1.Id,
                Type = "LOCATION",
                CanonicalName = "Pune Central",
                NormalizedValue = "Pune Central",
                Location = "Pune Central Logistics Hub",
                District = "Pune",
                City = "Pune",
                State = "Maharashtra",
                Country = "India",
                Address = "Shivajinagar Bus Station & Logistics Terminal",
                Latitude = 18.5204,
                Longitude = 73.8567,
                GeocodePrecision = "AREA",
                GeocodeSource = "SYNTHETIC_DEMO",
                VerificationStatus = "VERIFIED",
                Confidence = 0.92,
                RiskLevel = "MEDIUM",
                Description = "Meeting location for handover of encrypted burner hardware.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEntPuneDock = new EntityItem
            {
                Id = "can-ent-pune-dock",
                CaseId = case1.Id,
                Type = "LOCATION",
                CanonicalName = "Shivajinagar Docks",
                NormalizedValue = "SHIVAJINAGAR DOCKS",
                Location = "Shivajinagar Docks, Berth 4",
                District = "Pune",
                City = "Pune",
                State = "Maharashtra",
                Country = "India",
                Address = "Berth 4, Shivajinagar Wharf Corridor",
                Latitude = 18.5314,
                Longitude = 73.8446,
                GeocodePrecision = "BUILDING",
                GeocodeSource = "SYNTHETIC_DEMO",
                VerificationStatus = "VERIFIED",
                Confidence = 0.95,
                RiskLevel = "HIGH",
                Description = "Primary riverside logistics dock used for unmanifested cargo transit.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEnt5 = new EntityItem
            {
                Id = "can-ent-pune-005",
                CaseId = case1.Id,
                Type = "ORGANIZATION",
                CanonicalName = "Apex Trading Syndicate",
                NormalizedValue = "APEX TRADING SYNDICATE",
                VerificationStatus = "VERIFIED",
                Confidence = 0.90,
                RiskLevel = "CRITICAL",
                Description = "Front company used for layering funds through shell invoices.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEnt6 = new EntityItem
            {
                Id = "can-ent-pune-006",
                CaseId = case1.Id,
                Type = "ACCOUNT",
                CanonicalName = "ACC-9021-PUNE-01",
                NormalizedValue = "ACC9021PUNE01",
                AccountNumber = "ACC-9021-PUNE-01",
                BankName = "State Bank of India",
                VerificationStatus = "VERIFIED",
                Confidence = 0.96,
                RiskLevel = "HIGH",
                Description = "Mule account used for structured cash deposits under Rs 50,000 threshold.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEnt7 = new EntityItem
            {
                Id = "can-ent-pune-007",
                CaseId = case1.Id,
                Type = "PERSON",
                CanonicalName = "Sameer Patil",
                NormalizedValue = "Sameer Patil",
                VerificationStatus = "VERIFIED",
                Confidence = 0.94,
                RiskLevel = "HIGH",
                Location = "Camp, Pune",
                District = "Pune",
                Description = "Local operative managing cash distribution points across Pune district.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            // Case 2 Entities
            var canEntMum1 = new EntityItem
            {
                Id = "can-ent-mum-001",
                CaseId = case2.Id,
                Type = "PERSON",
                CanonicalName = "Vikram Singhania",
                NormalizedValue = "Vikram Singhania",
                VerificationStatus = "VERIFIED",
                Confidence = 0.95,
                RiskLevel = "HIGH",
                Location = "Colaba, Mumbai",
                District = "Mumbai City",
                Description = "Cargo clearing agent suspected of clearing illicit shipments at Mumbai port.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            var canEntMum2 = new EntityItem
            {
                Id = "can-ent-mum-002",
                CaseId = case2.Id,
                Type = "LOCATION",
                CanonicalName = "JNPT Port Mumbai",
                NormalizedValue = "JNPT Port Mumbai",
                Location = "Nhava Sheva, Navi Mumbai",
                District = "Navi Mumbai",
                VerificationStatus = "VERIFIED",
                Confidence = 0.97,
                RiskLevel = "CRITICAL",
                Description = "Container terminal where contraband seizure occurred.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            var canEntMum3 = new EntityItem
            {
                Id = "can-ent-mum-003",
                CaseId = case2.Id,
                Type = "PERSON",
                CanonicalName = "R. Sharma",
                NormalizedValue = "R. Sharma",
                VerificationStatus = "VERIFIED",
                Confidence = 0.94,
                RiskLevel = "HIGH",
                PhoneNumber = "+91-9000001001",
                VehicleNumber = "MH12AB4321",
                AliasesJson = "[\"Rahul Mehta\"]",
                Location = "Dock Yard Road, Mumbai",
                District = "Mumbai City",
                Description = "Logistics coordinator at Mumbai port container terminal. Corroborated with Pune network assets.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            // Case 3 Entity (Synthetic Demo False-Positive Baseline)
            var canEntNashik1 = new EntityItem
            {
                Id = "can-ent-nsk-001",
                CaseId = case3.Id,
                Type = "PERSON",
                CanonicalName = "Rahul Sharma",
                NormalizedValue = "Rahul Sharma",
                VerificationStatus = "VERIFIED",
                Confidence = 0.90,
                RiskLevel = "LOW",
                PhoneNumber = "+91-9888880000",
                VehicleNumber = "MH15XY9999",
                Location = "Panchavati, Nashik",
                District = "Nashik",
                Description = "Agricultural logistics driver operating in Nashik district corridor. No shared identifiers with Pune network.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
            };

            // Bridge Nexus Entity (Person C) connecting Cluster A (Rahul Mehta) with Cluster B (Sameer Patil)
            var canEntBridge = new EntityItem
            {
                Id = "can-ent-pune-008",
                CaseId = case1.Id,
                Type = "PERSON",
                CanonicalName = "Vikram Gaikwad",
                NormalizedValue = "Vikram Gaikwad",
                VerificationStatus = "VERIFIED",
                Confidence = 0.95,
                RiskLevel = "HIGH",
                Location = "Swargate, Pune",
                District = "Pune",
                Description = "Liaison operative observed coordinating cross-cluster communications.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEntPhone2 = new EntityItem
            {
                Id = "can-ent-pune-009",
                CaseId = case1.Id,
                Type = "PHONE",
                CanonicalName = "+91-9111112222",
                NormalizedValue = "+919111112222",
                VerificationStatus = "VERIFIED",
                Confidence = 0.97,
                PhoneNumber = "+91-9111112222",
                RiskLevel = "MEDIUM",
                Description = "Secondary burn phone line used for distribution logistics.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEntVehicle2 = new EntityItem
            {
                Id = "can-ent-pune-010",
                CaseId = case1.Id,
                Type = "VEHICLE",
                CanonicalName = "MH14CD5678",
                NormalizedValue = "MH14CD5678",
                VerificationStatus = "VERIFIED",
                Confidence = 0.94,
                VehicleNumber = "MH14CD5678",
                RiskLevel = "MEDIUM",
                Description = "Transport vehicle registered in Pimpri-Chinchwad.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canEntMumDock = new EntityItem
            {
                Id = "can-ent-mum-dock",
                CaseId = case2.Id,
                Type = "LOCATION",
                CanonicalName = "Mumbai Port Trust",
                NormalizedValue = "MUMBAI PORT TRUST",
                Location = "Yellow Gate, Mumbai Port",
                District = "Mumbai City",
                City = "Mumbai",
                State = "Maharashtra",
                Country = "India",
                Address = "Yellow Gate, Port Trust Logistics Yard",
                Latitude = 18.9438,
                Longitude = 72.8441,
                GeocodePrecision = "BUILDING",
                GeocodeSource = "SYNTHETIC_DEMO",
                VerificationStatus = "VERIFIED",
                Confidence = 0.96,
                RiskLevel = "CRITICAL",
                Description = "Container terminal associated with maritime cargo inspection.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            var canEntNashikLoc = new EntityItem
            {
                Id = "can-ent-nsk-loc",
                CaseId = case3.Id,
                Type = "LOCATION",
                CanonicalName = "Nashik Transit Corridor",
                NormalizedValue = "NASHIK TRANSIT CORRIDOR",
                Location = "Mumbai-Agra Highway Checkpoint",
                District = "Nashik",
                City = "Nashik",
                State = "Maharashtra",
                Country = "India",
                Address = "Mumbai-Agra Highway Checkpoint, Panchavati",
                Latitude = 19.9975,
                Longitude = 73.7898,
                GeocodePrecision = "AREA",
                GeocodeSource = "SYNTHETIC_DEMO",
                VerificationStatus = "VERIFIED",
                Confidence = 0.90,
                RiskLevel = "LOW",
                Description = "Control checkpoint in Nashik rural logistics corridor.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
            };

            context.Entities.AddRange(
                canEnt1, canEnt2, canEnt3, canEnt4, canEnt5, canEnt6, canEnt7,
                canEntBridge, canEntPhone2, canEntVehicle2, canEntPuneDock,
                canEntMum1, canEntMum2, canEntMum3, canEntMumDock,
                canEntNashik1, canEntNashikLoc
            );

            // Phase 7 Canonical Location Records
            var locPuneDock = new LocationItem
            {
                Id = "loc-pune-dock",
                CaseId = case1.Id,
                EntityId = canEntPuneDock.Id,
                Name = "Shivajinagar Docks, Berth 4",
                NormalizedName = "SHIVAJINAGAR DOCKS",
                Address = "Berth 4, Shivajinagar Wharf Corridor",
                City = "Pune",
                District = "Pune",
                State = "Maharashtra",
                Country = "India",
                Latitude = 18.5314,
                Longitude = 73.8446,
                GeocodePrecision = "BUILDING",
                Source = "SYNTHETIC_DEMO",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var locPuneCentral = new LocationItem
            {
                Id = "loc-pune-central",
                CaseId = case1.Id,
                EntityId = canEnt4.Id,
                Name = "Pune Central Logistics Hub",
                NormalizedName = "PUNE CENTRAL",
                Address = "Shivajinagar Bus Station & Logistics Terminal",
                City = "Pune",
                District = "Pune",
                State = "Maharashtra",
                Country = "India",
                Latitude = 18.5204,
                Longitude = 73.8567,
                GeocodePrecision = "AREA",
                Source = "SYNTHETIC_DEMO",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var locMumDock = new LocationItem
            {
                Id = "loc-mum-dock",
                CaseId = case2.Id,
                EntityId = canEntMumDock.Id,
                Name = "Mumbai Port Trust",
                NormalizedName = "MUMBAI PORT TRUST",
                Address = "Yellow Gate, Port Trust Logistics Yard",
                City = "Mumbai",
                District = "Mumbai City",
                State = "Maharashtra",
                Country = "India",
                Latitude = 18.9438,
                Longitude = 72.8441,
                GeocodePrecision = "BUILDING",
                Source = "SYNTHETIC_DEMO",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            var locNashik = new LocationItem
            {
                Id = "loc-nashik-corridor",
                CaseId = case3.Id,
                EntityId = canEntNashikLoc.Id,
                Name = "Nashik Transit Corridor",
                NormalizedName = "NASHIK TRANSIT CORRIDOR",
                Address = "Mumbai-Agra Highway Checkpoint, Panchavati",
                City = "Nashik",
                District = "Nashik",
                State = "Maharashtra",
                Country = "India",
                Latitude = 19.9975,
                Longitude = 73.7898,
                GeocodePrecision = "AREA",
                Source = "SYNTHETIC_DEMO",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
            };

            context.Locations.AddRange(locPuneDock, locPuneCentral, locMumDock, locNashik);
            await context.SaveChangesAsync();

            // 8. Canonical Verified Relationships
            var canRel1 = new Relationship
            {
                Id = "can-rel-pune-001",
                CaseId = case1.Id,
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEnt2.Id,
                Type = "COMMUNICATED_WITH",
                Confidence = 0.98,
                SourceEvidenceId = ev2.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canRel2 = new Relationship
            {
                Id = "can-rel-pune-002",
                CaseId = case1.Id,
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEnt3.Id,
                Type = "OPERATES",
                Confidence = 0.95,
                SourceEvidenceId = ev2.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canRel3 = new Relationship
            {
                Id = "can-rel-pune-003",
                CaseId = case1.Id,
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEnt4.Id,
                Type = "LOCATED_AT",
                Confidence = 0.92,
                SourceEvidenceId = ev2.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canRel4 = new Relationship
            {
                Id = "can-rel-pune-004",
                CaseId = case1.Id,
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEntBridge.Id,
                Type = "COMMUNICATED_WITH",
                Confidence = 0.94,
                SourceEvidenceId = ev2.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canRel5 = new Relationship
            {
                Id = "can-rel-pune-005",
                CaseId = case1.Id,
                SourceEntityId = canEnt7.Id,
                TargetEntityId = canEnt5.Id,
                Type = "MEMBER_OF",
                Confidence = 0.90,
                SourceEvidenceId = ev2.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canRel6 = new Relationship
            {
                Id = "can-rel-pune-006",
                CaseId = case1.Id,
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEnt6.Id,
                Type = "TRANSFERRED_MONEY_TO",
                Confidence = 0.96,
                SourceEvidenceId = ev1.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canRelBridge2 = new Relationship
            {
                Id = "can-rel-pune-007",
                CaseId = case1.Id,
                SourceEntityId = canEntBridge.Id,
                TargetEntityId = canEnt7.Id,
                Type = "ASSOCIATE_OF",
                Confidence = 0.93,
                SourceEvidenceId = ev2.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canRelClusterB1 = new Relationship
            {
                Id = "can-rel-pune-008",
                CaseId = case1.Id,
                SourceEntityId = canEnt7.Id,
                TargetEntityId = canEntPhone2.Id,
                Type = "COMMUNICATED_WITH",
                Confidence = 0.96,
                SourceEvidenceId = ev2.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var canRelClusterB2 = new Relationship
            {
                Id = "can-rel-pune-009",
                CaseId = case1.Id,
                SourceEntityId = canEnt7.Id,
                TargetEntityId = canEntVehicle2.Id,
                Type = "OPERATES",
                Confidence = 0.94,
                SourceEvidenceId = ev2.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            // Case 2 Relationship
            var canRelMum1 = new Relationship
            {
                Id = "can-rel-mum-001",
                CaseId = case2.Id,
                SourceEntityId = canEntMum1.Id,
                TargetEntityId = canEntMum2.Id,
                Type = "LOCATED_AT",
                Confidence = 0.95,
                SourceEvidenceId = ev3.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            context.Relationships.AddRange(canRel1, canRel2, canRel3, canRel4, canRel5, canRel6, canRelBridge2, canRelClusterB1, canRelClusterB2, canRelMum1);

            // 9. Multi-Evidence Provenance Records (Demonstrating multiple supporting evidence)
            var relEv1 = new RelationshipEvidence
            {
                Id = "relev-001",
                RelationshipId = canRel1.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                Confidence = 0.98,
                SourceLocation = "Line 3: \"Subject was observed making multiple phone calls using number +91-9000001001\"",
                VerifiedBy = "DCP Rajesh Sharma",
                VerifiedAtUtc = DateTime.UtcNow.AddDays(-12),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var relEv2 = new RelationshipEvidence
            {
                Id = "relev-002",
                RelationshipId = canRel1.Id,
                EvidenceId = ev1.Id, // Second supporting evidence item (CDR Records)
                CaseId = case1.Id,
                Confidence = 0.99,
                SourceLocation = "Call Records Row 2: Originating cell tower Shivajinagar connected to +919000001001",
                VerifiedBy = "DCP Rajesh Sharma",
                VerifiedAtUtc = DateTime.UtcNow.AddDays(-12),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var relEv3 = new RelationshipEvidence
            {
                Id = "relev-003",
                RelationshipId = canRel2.Id,
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                Confidence = 0.95,
                SourceLocation = "Line 2: \"Rahul Mehta entered blue sedan vehicle registration MH12AB4321\"",
                VerifiedBy = "DCP Rajesh Sharma",
                VerifiedAtUtc = DateTime.UtcNow.AddDays(-12),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            var relEv4 = new RelationshipEvidence
            {
                Id = "relev-004",
                RelationshipId = canRel6.Id,
                EvidenceId = ev1.Id,
                CaseId = case1.Id,
                Confidence = 0.96,
                SourceLocation = "Transaction Ledger: Remitted Rs 45,000 to SBI Account ACC-9021-PUNE-01",
                VerifiedBy = "DCP Rajesh Sharma",
                VerifiedAtUtc = DateTime.UtcNow.AddDays(-12),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
            };

            context.RelationshipEvidences.AddRange(relEv1, relEv2, relEv3, relEv4);
            await context.SaveChangesAsync();

            // 8b. Seed EntityMatchCandidates & CrossCaseConnections (Synthetic Demo Data)
            var seedCandidate1 = new EntityMatchCandidate
            {
                Id = "emc-seed-001",
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEntMum3.Id,
                SourceCaseId = case1.Id,
                TargetCaseId = case2.Id,
                EntityType = "PERSON",
                MatchStatus = "APPROVED",
                MatchScore = 0.94,
                MatchMethod = "MULTI_SIGNAL",
                MatchExplanationJson = System.Text.Json.JsonSerializer.Serialize(new object[]
                {
                    new
                    {
                        type = "SHARED_PHONE",
                        weight = 0.45,
                        score = 1.0,
                        description = "Identical normalized cellular subscriber: +919000001001",
                        evidenceCitations = new object[]
                        {
                            new { evidenceId = ev1.Id, caseId = case1.Id, fileName = ev1.FileName, evidenceType = "text/csv", sourceLocation = "Row 2", extractedQuote = "+91-9000001001 call record" }
                        }
                    },
                    new
                    {
                        type = "SHARED_VEHICLE",
                        weight = 0.35,
                        score = 1.0,
                        description = "Identical transport registration: MH12AB4321",
                        evidenceCitations = new object[]
                        {
                            new { evidenceId = ev2.Id, caseId = case1.Id, fileName = ev2.FileName, evidenceType = "text/plain", sourceLocation = "Page 1", extractedQuote = "Rahul Mehta entered vehicle MH12AB4321" }
                        }
                    },
                    new
                    {
                        type = "NAME_SIMILARITY",
                        weight = 0.30,
                        score = 0.91,
                        description = "High token name similarity: 'Rahul Mehta' / 'R. Sharma'",
                        evidenceCitations = Array.Empty<object>()
                    },
                    new
                    {
                        type = "ALIAS_MATCH",
                        weight = 0.25,
                        score = 1.0,
                        description = "Documented alias 'Rahul Mehta' cross-referenced across cases",
                        evidenceCitations = Array.Empty<object>()
                    }
                }),
                ReviewedBy = dcpSharma.FullName,
                ReviewedAtUtc = DateTime.UtcNow.AddDays(-2),
                ReviewNotes = "Confirmed cross-case link based on shared burner telephone and vehicle intercept.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
            };

            var seedCandidate2 = new EntityMatchCandidate
            {
                Id = "emc-seed-002",
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEntNashik1.Id,
                SourceCaseId = case1.Id,
                TargetCaseId = case3.Id,
                EntityType = "PERSON",
                MatchStatus = "PENDING",
                MatchScore = 0.45,
                MatchMethod = "FUZZY_NAME",
                MatchExplanationJson = System.Text.Json.JsonSerializer.Serialize(new object[]
                {
                    new
                    {
                        type = "NAME_SIMILARITY",
                        weight = 0.30,
                        score = 0.85,
                        description = "Name token similarity between 'Rahul Mehta' and 'Rahul Sharma'",
                        evidenceCitations = Array.Empty<object>()
                    },
                    new
                    {
                        type = "CONTRADICTORY_ATTRIBUTE",
                        weight = -0.20,
                        score = 1.0,
                        description = "Conflicting geographic jurisdiction: 'Pune' vs 'Nashik' with no shared unique telephone or vehicle",
                        evidenceCitations = Array.Empty<object>()
                    }
                }),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            var seedConn1 = new CrossCaseConnection
            {
                Id = "ccc-seed-001",
                CandidateId = seedCandidate1.Id,
                SourceCaseId = case1.Id,
                TargetCaseId = case2.Id,
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEntMum3.Id,
                ConnectionType = "MULTI_SIGNAL_CONNECTION",
                Confidence = 0.94,
                Status = "APPROVED",
                Explanation = "Investigator-verified cross-case nexus connecting Rahul Mehta (CASE-2026-001) and R. Sharma (CASE-2026-002). Corroborated by cellular subscriber +919000001001 and vehicle MH12AB4321.",
                ReviewedBy = dcpSharma.FullName,
                ReviewedAtUtc = DateTime.UtcNow.AddDays(-2),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
            };

            context.EntityMatchCandidates.AddRange(seedCandidate1, seedCandidate2);
            context.CrossCaseConnections.Add(seedConn1);
            await context.SaveChangesAsync();

            // 7. Phase 5 Seeded Graph Analytics Run & Model Leads
            var seedRun1 = new GraphAnalysisRun
            {
                Id = "gar-demo-001",
                CaseId = case1.Id,
                Status = "COMPLETED",
                StartedAtUtc = DateTime.UtcNow.AddDays(-1),
                CompletedAtUtc = DateTime.UtcNow.AddDays(-1).AddSeconds(4),
                NodeCount = 10,
                EdgeCount = 9,
                MetricsGenerated = 10,
                ModelVersion = "GAT-v1.0.0",
                NetworkDensity = 0.20,
                AverageDegree = 1.80,
                AveragePathLength = 2.45,
                ConnectedComponentsCount = 1,
                CommunitiesCount = 2,
                ExecutedBy = dcpSharma.Id,
                ConfigurationJson = "{\"includeCrossCase\":false,\"gatEmbeddingDim\":64,\"gatHeads\":4}"
            };

            var seedMetric1 = new GraphNodeMetrics
            {
                Id = "gnm-seed-001",
                AnalysisRunId = seedRun1.Id,
                EntityId = canEnt1.Id,
                CaseId = case1.Id,
                Degree = 4,
                NormalizedDegree = 0.444,
                BetweennessCentrality = 0.556,
                ClosenessCentrality = 0.60,
                PageRank = 0.22,
                ComponentId = "component-1",
                CommunityId = "cluster-1",
                AnalyticalIndicator = "High Connectivity Lead",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            var seedMetricBridge = new GraphNodeMetrics
            {
                Id = "gnm-seed-bridge",
                AnalysisRunId = seedRun1.Id,
                EntityId = canEntBridge.Id,
                CaseId = case1.Id,
                Degree = 2,
                NormalizedDegree = 0.222,
                BetweennessCentrality = 0.667,
                ClosenessCentrality = 0.643,
                PageRank = 0.16,
                ComponentId = "component-1",
                CommunityId = "cluster-1",
                AnalyticalIndicator = "Key Nexus Entity",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            var seedMetric7 = new GraphNodeMetrics
            {
                Id = "gnm-seed-007",
                AnalysisRunId = seedRun1.Id,
                EntityId = canEnt7.Id,
                CaseId = case1.Id,
                Degree = 4,
                NormalizedDegree = 0.444,
                BetweennessCentrality = 0.556,
                ClosenessCentrality = 0.60,
                PageRank = 0.20,
                ComponentId = "component-1",
                CommunityId = "cluster-2",
                AnalyticalIndicator = "High Connectivity Lead",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            var seedLead1 = new GraphAnalyticalLead
            {
                Id = "gal-demo-001",
                CaseId = case1.Id,
                AnalysisRunId = seedRun1.Id,
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEntPhone2.Id,
                LeadType = "POTENTIAL_RELATIONSHIP",
                SuggestedRelationshipType = "COMMUNICATED_WITH",
                Score = 0.87,
                Status = "PENDING",
                ModelVersion = "GAT-v1.0.0",
                ExplanationJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    cosineSimilarity = 0.84,
                    sharedNeighborsCount = 2,
                    sharedNeighborNames = new[] { "Vikram Gaikwad", "SameER Patil" },
                    attentionWeight = 0.76,
                    contributingSignals = new object[]
                    {
                        new { signalName = "Latent Embedding Similarity", weight = 0.50, contribution = 0.42, description = "Cosine similarity of 0.84 in GAT latent space." },
                        new { signalName = "Shared Network Context", weight = 0.35, contribution = 0.35, description = "2 common verified neighboring entities connecting both nodes." },
                        new { signalName = "Topological Jaccard Overlap", weight = 0.15, contribution = 0.10, description = "Neighborhood intersection across direct network pathways." }
                    }
                }),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            context.GraphAnalysisRuns.Add(seedRun1);
            context.GraphNodeMetrics.AddRange(seedMetric1, seedMetricBridge, seedMetric7);
            context.GraphAnalyticalLeads.Add(seedLead1);
            await context.SaveChangesAsync();

            // 7c. Phase 6 Temporal Events & Baseline Analysis
            var evT1 = new ExtractedEvent
            {
                Id = "evt-20260310-001",
                ExtractionJobId = "job-20260814-001",
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EventType = "LOCATION_ACTIVITY",
                Description = "Subject Rahul Sharma observed arriving at Shivajinagar Docks maritime cargo perimeter.",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                TimePrecision = "EXACT",
                Location = "Shivajinagar Docks",
                LocationEntityId = locPuneDock.Id,
                RelatedEntitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Rahul Sharma", "Shivajinagar Docks" }),
                Confidence = 0.96,
                SourceLocation = "Page 1, Paragraph 2",
                SourcePage = 1,
                ReviewStatus = "APPROVED",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            var evT2 = new ExtractedEvent
            {
                Id = "evt-20260310-002",
                ExtractionJobId = "job-20260814-001",
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EventType = "VISIT",
                Description = "Vikram Gaikwad verified entering gate terminal at Shivajinagar Docks.",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 30, 0, DateTimeKind.Utc),
                TimePrecision = "EXACT",
                Location = "Shivajinagar Docks",
                LocationEntityId = locPuneDock.Id,
                RelatedEntitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Vikram Gaikwad", "Shivajinagar Docks" }),
                Confidence = 0.94,
                SourceLocation = "Page 2, Gate Registry",
                SourcePage = 2,
                ReviewStatus = "APPROVED",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            var evT3 = new ExtractedEvent
            {
                Id = "evt-20260312-003",
                ExtractionJobId = "job-20260814-001",
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EventType = "TRANSACTION",
                Description = "Cash remittance transaction recorded at Pune Central financial node.",
                StartTimeUtc = new DateTime(2026, 3, 12, 14, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 12, 14, 15, 0, DateTimeKind.Utc),
                TimePrecision = "MINUTE",
                Location = "Pune Central",
                LocationEntityId = locPuneCentral.Id,
                RelatedEntitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Sameer Patil", "ACC-9021-PUNE-01", "Pune Central" }),
                Confidence = 0.92,
                SourceLocation = "Page 3, Ledger line 44",
                SourcePage = 3,
                ReviewStatus = "APPROVED",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-8)
            };

            var evT4 = new ExtractedEvent
            {
                Id = "evt-20260315-004",
                ExtractionJobId = "job-20260814-001",
                EvidenceId = ev2.Id,
                CaseId = case1.Id,
                EventType = "VEHICLE_SIGHTING",
                Description = "Vehicle MH12AB4321 spotted traversing industrial transit corridor in Pune.",
                StartTimeUtc = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 15, 9, 30, 0, DateTimeKind.Utc),
                TimePrecision = "HOUR",
                Location = "Pune Central",
                LocationEntityId = locPuneCentral.Id,
                RelatedEntitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { "MH12AB4321", "Pune Central" }),
                Confidence = 0.89,
                SourceLocation = "Traffic Log ANPR #82",
                SourcePage = 1,
                ReviewStatus = "APPROVED",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
            };

            var evT5 = new ExtractedEvent
            {
                Id = "evt-20260304-005",
                ExtractionJobId = "job-20260814-001",
                EvidenceId = ev1.Id,
                CaseId = case1.Id,
                EventType = "DOCUMENT_EVENT",
                Description = "Syndicate Corp incorporation and corporate charter filed.",
                StartTimeUtc = new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 4, 23, 59, 59, DateTimeKind.Utc),
                TimePrecision = "DATE_ONLY",
                Location = "Pune",
                LocationEntityId = locPuneCentral.Id,
                RelatedEntitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Apex Trading Syndicate" }),
                Confidence = 0.98,
                SourceLocation = "Registrar Filing Exhibit A",
                SourcePage = 1,
                ReviewStatus = "APPROVED",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-15)
            };

            // Cross-case event in Case 2 (Coincides at Shivajinagar Docks on 2026-03-10 from 10:45 to 12:00)
            var evMumT1 = new ExtractedEvent
            {
                Id = "evt-mum-20260310-001",
                ExtractionJobId = "job-mum-001",
                EvidenceId = ev3.Id,
                CaseId = case2.Id,
                EventType = "COMMUNICATION",
                Description = "Cellular tower intercept logged for terminal +91-9000001001 near Shivajinagar Docks sector.",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 45, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                TimePrecision = "MINUTE",
                Location = "Shivajinagar Docks",
                LocationEntityId = locPuneDock.Id,
                RelatedEntitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { "R. Sharma", "+91-9000001001", "Shivajinagar Docks" }),
                Confidence = 0.93,
                SourceLocation = "Tower Intercept CSV row 108",
                SourcePage = 1,
                ReviewStatus = "APPROVED",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
            };

            context.ExtractedEvents.AddRange(evT1, evT2, evT3, evT4, evT5, evMumT1);
            await context.SaveChangesAsync();

            var seedTemporalRun = new TemporalAnalysisRun
            {
                Id = "run-t-demo-001",
                CaseId = case1.Id,
                Status = "COMPLETED",
                StartedAtUtc = DateTime.UtcNow.AddDays(-1),
                CompletedAtUtc = DateTime.UtcNow.AddDays(-1).AddSeconds(2),
                TotalEventsAnalyzed = 5,
                SignalsGenerated = 1,
                OverlapsFound = 1,
                ClustersFound = 2,
                ExecutedBy = dcpSharma.FullName,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            var seedTemporalSignal = new TemporalSignal
            {
                Id = "sig-t-demo-001",
                CaseId = case1.Id,
                AnalysisRunId = seedTemporalRun.Id,
                SignalType = "TEMPORAL_OVERLAP",
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEntBridge.Id,
                LocationEntityId = locPuneDock.Id,
                LocationName = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 30.0,
                Score = 0.88,
                Explanation = "Activity windows coincide at Shivajinagar Docks from 2026-03-10 10:30:00 UTC to 2026-03-10 11:00:00 UTC (30 minutes overlap duration). Note: Coincident activity window indicates temporal correlation, not confirmed contact.",
                Status = "PENDING",
                SignalsJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    overlapStartUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                    overlapEndUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                    durationMinutes = 30.0,
                    event1Id = evT1.Id,
                    event2Id = evT2.Id,
                    evidence1Id = ev2.Id,
                    evidence2Id = ev2.Id,
                    location = "Shivajinagar Docks"
                }),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            context.TemporalAnalysisRuns.Add(seedTemporalRun);
            context.TemporalSignals.Add(seedTemporalSignal);

            // Phase 7 Baseline Spatial Analysis Run & Signal
            var seedSpatialRun = new SpatialAnalysisRun
            {
                Id = "sar-demo-001",
                CaseId = case1.Id,
                Status = "COMPLETED",
                StartedAtUtc = DateTime.UtcNow.AddDays(-1),
                CompletedAtUtc = DateTime.UtcNow.AddDays(-1).AddSeconds(2),
                TotalLocationsAnalyzed = 2,
                TotalEventsAnalyzed = 5,
                SignalsGenerated = 1,
                OverlapsFound = 1,
                ClustersFound = 1,
                VelocityWarningsFound = 0,
                ExecutedBy = dcpSharma.FullName
            };

            var seedSpatialSignal = new SpatialSignal
            {
                Id = "sig-spat-demo-001",
                CaseId = case1.Id,
                AnalysisRunId = seedSpatialRun.Id,
                SignalType = "SPATIAL_TEMPORAL_OVERLAP",
                SourceEntityId = canEnt1.Id,
                TargetEntityId = canEntBridge.Id,
                LocationId = locPuneDock.Id,
                LocationName = locPuneDock.Name,
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                DistanceKm = 0.0,
                Score = 0.92,
                Explanation = "Both entities had recorded activity at Shivajinagar Docks during an overlapping temporal window (30 minutes). Note: Investigative signal, not proof of criminal contact.",
                SupportingEvidenceJson = System.Text.Json.JsonSerializer.Serialize(new[] { ev2.Id }),
                Status = "PENDING",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            context.SpatialAnalysisRuns.Add(seedSpatialRun);
            context.SpatialSignals.Add(seedSpatialSignal);
            await context.SaveChangesAsync();

            // 7. Phase 8 Baseline Alerts and Anomaly Detection Run
            var seedAlertRun = new AlertRun
            {
                Id = "run-alert-demo-001",
                CaseId = case1.Id,
                Status = "COMPLETED",
                ExecutedBy = dcpSharma.FullName,
                DetectorsExecuted = 9,
                SignalsGenerated = 3,
                AlertsCreated = 3,
                AlertsDeduplicated = 0,
                ExecutionDurationMs = 45,
                StartedAtUtc = DateTime.UtcNow.AddHours(-6),
                CompletedAtUtc = DateTime.UtcNow.AddHours(-6).AddMilliseconds(45)
            };

            var seedAlert1 = new Alert
            {
                Id = "alt-demo-001",
                CaseId = case1.Id,
                AlertRunId = seedAlertRun.Id,
                AlertType = "NETWORK_ANOMALY",
                Severity = "HIGH",
                Status = "NEW",
                Title = "High Connectivity Nexus Entity: Rahul Sharma",
                Description = "Entity recorded 3 direct relationships and betweenness centrality of 0.42, serving as a structural hub across the investigation network.",
                SourceEntityId = canEnt1.Id,
                SourceEntityName = "Rahul Sharma",
                Score = 0.88,
                DetectionMethod = "DegreeCentralityDisparity",
                DetectionVersion = "v1.0",
                Explanation = $"WHAT: High relationship density observed.\nWHO: Rahul Sharma (ID: {canEnt1.Id})\nMETRICS: Degree = 3 (Case Avg = 1.6)\nWHY: Marked structural hub position in the investigation knowledge graph warrants investigative review.\nNOTE: Anomaly is an investigative signal, not proof of criminal activity.",
                DeduplicationFingerprint = "SHA256:DEMO-NET-RAHUL-SHARMA",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-6)
            };

            var seedAlert2 = new Alert
            {
                Id = "alt-demo-002",
                CaseId = case1.Id,
                AlertRunId = seedAlertRun.Id,
                AlertType = "GEOGRAPHIC_ANOMALY",
                Severity = "HIGH",
                Status = "NEW",
                Title = "Spatial Co-Presence Signal: Shivajinagar Docks",
                Description = "Spatial co-presence detected between Rahul Sharma and Vikram Gaikwad at Shivajinagar Docks during overlapping temporal window.",
                SourceEntityId = canEnt1.Id,
                SourceEntityName = "Rahul Sharma",
                TargetEntityId = canEntBridge.Id,
                TargetEntityName = "Vikram Gaikwad",
                LocationId = locPuneDock.Id,
                LocationName = locPuneDock.Name,
                RelatedEvidenceId = ev2.Id,
                RelatedEvidenceFileName = ev2.FileName,
                RelatedEvidenceSha256 = ev2.Sha256Hash,
                Score = 0.92,
                DetectionMethod = "SpatialSignalBridge",
                DetectionVersion = "v1.0",
                Explanation = "WHAT: Spatial co-presence detected.\nWHERE: Shivajinagar Docks (Pune)\nENTITIES: Rahul Sharma & Vikram Gaikwad\nSCORE: 0.92\nEVIDENCE: surveillance_report_pune.txt\nNOTE: Physical proximity is an investigative lead, not proof of criminal contact.",
                DeduplicationFingerprint = "SHA256:DEMO-GEO-SHIVAJINAGAR-DOCKS",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-6)
            };

            var seedAlert3 = new Alert
            {
                Id = "alt-demo-003",
                CaseId = case1.Id,
                AlertRunId = seedAlertRun.Id,
                AlertType = "CROSS_CASE_PATTERN",
                Severity = "MEDIUM",
                Status = "ACKNOWLEDGED",
                Title = "Cross-Case Entity Nexus: Rahul Sharma",
                Description = $"Entity 'Rahul Sharma' appears synchronously in authorized case {case2.CaseNumber} with confidence 95%.",
                SourceEntityId = canEnt1.Id,
                SourceEntityName = "Rahul Sharma",
                Score = 0.95,
                DetectionMethod = "CrossCaseEntityNexusResolution",
                DetectionVersion = "v1.0",
                Explanation = $"WHAT: Shared entity observed across distinct investigative cases.\nENTITY: Rahul Sharma\nMATCHED CASE: {case2.CaseNumber}\nWHY: Cross-case nexus indicates common operational lead across Western Maharashtra corridors.",
                DeduplicationFingerprint = "SHA256:DEMO-CROSSCASE-RAHUL",
                ReviewedAtUtc = DateTime.UtcNow.AddHours(-3),
                ReviewedBy = dcpSharma.FullName,
                ReviewNotes = "Acknowledged cross-case nexus with Mumbai port team.",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-3)
            };

            context.AlertRuns.Add(seedAlertRun);
            context.Alerts.AddRange(seedAlert1, seedAlert2, seedAlert3);

            // 8. Initial Audit Records
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    Id = "aud-init-001",
                    ActorId = dcpSharma.Id,
                    ActorName = dcpSharma.FullName,
                    Action = "CASE_CREATED",
                    ResourceType = "Case",
                    ResourceId = case1.Id,
                    MetadataJson = "{\"caseNumber\":\"CASE-2026-001\",\"title\":\"Operation Pune Hawala & Cyber Network\"}",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-15)
                },
                new AuditLog
                {
                    Id = "aud-init-002",
                    ActorId = dcpSharma.Id,
                    ActorName = dcpSharma.FullName,
                    Action = "EVIDENCE_UPLOADED",
                    ResourceType = "Evidence",
                    ResourceId = ev2.Id,
                    MetadataJson = "{\"fileName\":\"surveillance_report_pune.txt\",\"sha256\":\"7c2c73dfdc485cbf788c2ed0f27e6be2a0859a9024ea3723aaf3cc18f6369168\",\"version\":1}",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
                },
                new AuditLog
                {
                    Id = "aud-init-003",
                    ActorId = dcpSharma.Id,
                    ActorName = dcpSharma.FullName,
                    Action = "EVIDENCE_EXTRACTION_COMPLETED",
                    ResourceType = "Evidence",
                    ResourceId = ev2.Id,
                    MetadataJson = "{\"jobId\":\"job-20260814-001\",\"entitiesCount\":6,\"relationshipsCount\":3}",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-12).AddSeconds(5)
                }
            );
            await context.SaveChangesAsync();

            // 17. Evidence Integrity Ledger (Phase 9 — Deterministic Genesis & Authentic Evidence Blocks)
            var genesisHash = IntegrityLedgerService.ComputeCanonicalBlockHash(
                0,
                null,
                0,
                IntegrityLedgerService.GenesisEvidenceHash,
                IntegrityLedgerService.GenesisPreviousBlockHash,
                IntegrityLedgerService.GenesisAction,
                IntegrityLedgerService.GenesisActorId,
                IntegrityLedgerService.GenesisTimestampUtc,
                IntegrityLedgerService.GenesisMetadata);

            var block0 = new EvidenceLedgerBlock
            {
                Id = "block-genesis-000000",
                BlockIndex = 0,
                EvidenceItemId = null,
                EvidenceVersion = 0,
                EvidenceHash = IntegrityLedgerService.GenesisEvidenceHash.ToLowerInvariant(),
                PreviousBlockHash = IntegrityLedgerService.GenesisPreviousBlockHash.ToLowerInvariant(),
                BlockHash = genesisHash,
                Action = IntegrityLedgerService.GenesisAction,
                ActorUserId = IntegrityLedgerService.GenesisActorId,
                ActorName = IntegrityLedgerService.GenesisActorName,
                TimestampUtc = IntegrityLedgerService.GenesisTimestampUtc,
                MetadataJson = IntegrityLedgerService.GenesisMetadata,
                CreatedAtUtc = DateTime.UtcNow
            };

            var block1Meta = $"{{\"fileName\":\"{ev1.FileName}\",\"version\":{ev1.Version}}}";
            var block1Hash = IntegrityLedgerService.ComputeCanonicalBlockHash(
                1,
                ev1.Id,
                ev1.Version,
                ev1.Sha256Hash,
                block0.BlockHash,
                "UPLOAD",
                ev1.UploadedById ?? "usr-dcp-sharma",
                ev1.UploadedAtUtc,
                block1Meta);

            var block1 = new EvidenceLedgerBlock
            {
                Id = "block-000001-ev1",
                BlockIndex = 1,
                EvidenceItemId = ev1.Id,
                EvidenceVersion = ev1.Version,
                EvidenceHash = ev1.Sha256Hash.ToLowerInvariant(),
                PreviousBlockHash = block0.BlockHash.ToLowerInvariant(),
                BlockHash = block1Hash,
                Action = "UPLOAD",
                ActorUserId = ev1.UploadedById ?? "usr-dcp-sharma",
                ActorName = ev1.UploadedByName,
                TimestampUtc = ev1.UploadedAtUtc,
                MetadataJson = block1Meta,
                CreatedAtUtc = DateTime.UtcNow
            };

            var block2Meta = $"{{\"fileName\":\"{ev2.FileName}\",\"version\":{ev2.Version}}}";
            var block2Hash = IntegrityLedgerService.ComputeCanonicalBlockHash(
                2,
                ev2.Id,
                ev2.Version,
                ev2.Sha256Hash,
                block1.BlockHash,
                "UPLOAD",
                ev2.UploadedById ?? "usr-dcp-sharma",
                ev2.UploadedAtUtc,
                block2Meta);

            var block2 = new EvidenceLedgerBlock
            {
                Id = "block-000002-ev2",
                BlockIndex = 2,
                EvidenceItemId = ev2.Id,
                EvidenceVersion = ev2.Version,
                EvidenceHash = ev2.Sha256Hash.ToLowerInvariant(),
                PreviousBlockHash = block1.BlockHash.ToLowerInvariant(),
                BlockHash = block2Hash,
                Action = "UPLOAD",
                ActorUserId = ev2.UploadedById ?? "usr-dcp-sharma",
                ActorName = ev2.UploadedByName,
                TimestampUtc = ev2.UploadedAtUtc,
                MetadataJson = block2Meta,
                CreatedAtUtc = DateTime.UtcNow
            };

            var block3Meta = $"{{\"fileName\":\"{ev3.FileName}\",\"version\":{ev3.Version}}}";
            var block3Hash = IntegrityLedgerService.ComputeCanonicalBlockHash(
                3,
                ev3.Id,
                ev3.Version,
                ev3.Sha256Hash,
                block2.BlockHash,
                "UPLOAD",
                ev3.UploadedById ?? "usr-analyst-patil",
                ev3.UploadedAtUtc,
                block3Meta);

            var block3 = new EvidenceLedgerBlock
            {
                Id = "block-000003-ev3",
                BlockIndex = 3,
                EvidenceItemId = ev3.Id,
                EvidenceVersion = ev3.Version,
                EvidenceHash = ev3.Sha256Hash.ToLowerInvariant(),
                PreviousBlockHash = block2.BlockHash.ToLowerInvariant(),
                BlockHash = block3Hash,
                Action = "UPLOAD",
                ActorUserId = ev3.UploadedById ?? "usr-analyst-patil",
                ActorName = ev3.UploadedByName,
                TimestampUtc = ev3.UploadedAtUtc,
                MetadataJson = block3Meta,
                CreatedAtUtc = DateTime.UtcNow
            };

            context.EvidenceLedgerBlocks.AddRange(block0, block1, block2, block3);

            await context.SaveChangesAsync();
            logger.LogInformation("Database successfully seeded with Phase 9 Evidence Integrity Ledger blocks.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed database during initialization: {Message}", ex.Message);
        }
    }
}
