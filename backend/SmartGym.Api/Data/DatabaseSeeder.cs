using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Data;

public class DatabaseSeeder
{
    private readonly SmartGymDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(SmartGymDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SmartGym database seeding...");

        // Ensure database exists
        await _context.Database.MigrateAsync(cancellationToken);

        // 1. Roles
        var roleAdmin = await EnsureRoleAsync("Admin", "Full system administrator with unrestricted access.");
        var roleTrainer = await EnsureRoleAsync("Trainer", "Fitness trainer managing classes and guiding member workouts.");
        var roleMember = await EnsureRoleAsync("Member", "Registered gym member with access to facilities and bookings.");

        // 2. Users
        var adminUser = await EnsureUserAsync(
            "admin@smartgym.com",
            "Alexander",
            "Silva",
            "+94 77 123 4567",
            "Admin123!",
            roleAdmin);

        var trainerUser = await EnsureUserAsync(
            "trainer@smartgym.com",
            "Kavinda",
            "Fernando",
            "+94 71 987 6543",
            "Trainer123!",
            roleTrainer);

        var memberUser = await EnsureUserAsync(
            "member@smartgym.com",
            "Nuwan",
            "Perera",
            "+94 76 555 4321",
            "Member123!",
            roleMember);

        // 3. Member Profile
        var memberProfile = await EnsureMemberProfileAsync(
            memberUser.Id,
            emergencyContactName: "Anusha Perera",
            emergencyContactPhone: "+94 76 555 9999",
            gender: "Male",
            address: "No 78/2, Havelock Road, Colombo 05",
            medicalConditions: "None reported. Mild past meniscus injury (fully healed).");

        // 4. Membership Plans
        var bronzePlan = await EnsureMembershipPlanAsync("Bronze Tier", "Standard gym floor access during off-peak hours.", 6500.00m, 30, 2, false);
        var silverPlan = await EnsureMembershipPlanAsync("Silver Tier", "All-day gym floor access with 4 group fitness classes per week.", 17500.00m, 90, 4, false);
        var goldPlan = await EnsureMembershipPlanAsync("Gold Tier", "Full gym and studio access with unlimited classes and monthly fitness review.", 32000.00m, 180, 7, true);
        var platinumPlan = await EnsureMembershipPlanAsync("Platinum VIP", "All-inclusive VIP access including recovery suite, personal trainer, and supplement discounts.", 58000.00m, 365, 14, true);

        // Assign active Gold Tier membership to Nuwan
        await EnsureMembershipAsync(memberProfile.Id, goldPlan.Id, 32000.00m, 180);

        // 5. Goals & Progress Records for Member
        await EnsureMemberGoalsAndProgressAsync(memberProfile.Id, trainerUser.Id);

        // 6. Locations
        var locCardio = await EnsureLocationAsync("Cardio Zone A", "Ground Floor", "High-performance cardio equipment: treadmills, ellipticals, and rowers.");
        var locWeights = await EnsureLocationAsync("Free Weights Area", "Ground Floor", "Olympic barbells, dumbbells up to 50kg, benches, and power racks.");
        var locStudio1 = await EnsureLocationAsync("Studio 1 — High Intensity", "1st Floor", "Shock-absorbent flooring for HIIT, spin cycling, and functional training.");
        var locStudio2 = await EnsureLocationAsync("Studio 2 — Mind & Body", "1st Floor", "Heated yoga room, Pilates reformers, and mobility equipment.");
        var locRecovery = await EnsureLocationAsync("Recovery & Wellness Suite", "Basement", "Infrared sauna, cold plunge tubs, and sports massage therapy.");

        // 7. Equipment
        var treadmill1 = await EnsureEquipmentAsync(
            locCardio.Id,
            "LF-TRD-2023-001",
            "LifeFitness Elevation Treadmill T1",
            "Elevation T95",
            "LifeFitness USA",
            new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            EquipmentStatus.Operational,
            new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc));

        var treadmill2 = await EnsureEquipmentAsync(
            locCardio.Id,
            "LF-TRD-2023-002",
            "LifeFitness Elevation Treadmill T2",
            "Elevation T95",
            "LifeFitness USA",
            new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            EquipmentStatus.NeedsMaintenance,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        var powerRack = await EnsureEquipmentAsync(
            locWeights.Id,
            "HS-PR-2022-014",
            "Hammer Strength HD Elite Power Rack",
            "HD-Elite",
            "Hammer Strength",
            new DateTime(2022, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            EquipmentStatus.Operational,
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc));

        var skillmill = await EnsureEquipmentAsync(
            locCardio.Id,
            "TG-SKM-2024-003",
            "Technogym Skillmill Connect",
            "Skillmill Connect",
            "Technogym Italy",
            new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EquipmentStatus.UnderRepair,
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        // 8. Facility Issues, AI Workflow, and Maintenance History
        await EnsureFacilityIssueAndAIWorkflowAsync(memberProfile.Id, treadmill2.Id, locCardio.Id);
        await EnsureRepairOrderAndApprovalAsync(skillmill.Id, locCardio.Id, memberProfile.Id, adminUser.Id);

        // 9. Classes, Schedules, and Bookings
        var catHiit = await EnsureClassCategoryAsync("High-Intensity Interval Training (HIIT)", "Cardiovascular and strength interval circuits.");
        var catStrength = await EnsureClassCategoryAsync("Strength & Conditioning", "Hypertrophy, compound lifting, and power movements.");
        var catYoga = await EnsureClassCategoryAsync("Yoga & Mobility", "Flexibility, core stabilization, and active recovery.");

        var hiitClass = await EnsureFitnessClassAsync(catHiit.Id, "Metabolic Blast HIIT", "Fast-paced full body functional circuit burning up to 600 calories.", 45, 18, "High");
        var strengthClass = await EnsureFitnessClassAsync(catStrength.Id, "Power & Hypertrophy", "Focus on squat, bench press, deadlift, and accessory hypertrophy.", 60, 15, "High");
        var yogaClass = await EnsureFitnessClassAsync(catYoga.Id, "Sunrise Vinyasa Flow", "Dynamic flow connecting breath with posture to awaken muscles.", 60, 20, "Low");

        await EnsureSchedulesAndBookingsAsync(hiitClass.Id, strengthClass.Id, yogaClass.Id, trainerUser.Id, memberProfile.Id);

        // 10. Suppliers, Products, and Inventory
        var suppNutri = await EnsureSupplierAsync("NutriFit Lanka Pvt Ltd", "Rohan Jayasuriya", "sales@nutrifitlanka.com", "+94 11 234 5678", "No 45, Galle Road, Colombo 03");
        var suppApex = await EnsureSupplierAsync("Apex Gym Equipment & Spares", "Dinesh Fernando", "parts@apexgym.lk", "+94 11 445 6789", "No 120, Kandy Road, Kelaniya");

        var catProtein = await EnsureProductCategoryAsync("Protein Powders", "Whey isolate, concentrate, and plant-based protein blends.");
        var catPre = await EnsureProductCategoryAsync("Pre-Workouts & Amino Acids", "Energy formulas, nitric oxide boosters, and BCAAs.");
        var catAccessory = await EnsureProductCategoryAsync("Gym Accessories", "Lifting belts, wrist wraps, straps, and shaker bottles.");

        var prodWhey = await EnsureProductWithInventoryAsync(
            suppNutri.Id, catProtein.Id,
            "ON-WHEY-5LB-CHOC",
            "Optimum Nutrition Gold Standard 100% Whey 5 lbs (Double Rich Chocolate)",
            "World's best-selling whey protein delivering 24g protein per serving.",
            26500.00m, 21000.00m,
            quantityInStock: 24, reorderThreshold: 8, maxStock: 60, binLocation: "Aisle-1-A1",
            adminUserId: adminUser.Id);

        var prodIso = await EnsureProductWithInventoryAsync(
            suppNutri.Id, catProtein.Id,
            "DYM-ISO-5LB-CHOC",
            "Dymatize ISO100 Hydrolyzed 5 lbs (Gourmet Chocolate)",
            "Ultra-pure hydrolyzed whey protein isolate for rapid post-workout digestion.",
            31500.00m, 25000.00m,
            quantityInStock: 14, reorderThreshold: 6, maxStock: 40, binLocation: "Aisle-1-A2",
            adminUserId: adminUser.Id);

        var prodC4 = await EnsureProductWithInventoryAsync(
            suppNutri.Id, catPre.Id,
            "C4-PRE-30S-FP",
            "Cellucor C4 Original Pre-Workout 30 Servings (Fruit Punch)",
            "Explosive energy and muscular endurance booster featuring CarnoSyn Beta-Alanine.",
            11500.00m, 8500.00m,
            quantityInStock: 32, reorderThreshold: 10, maxStock: 80, binLocation: "Aisle-2-B1",
            adminUserId: adminUser.Id);

        var prodStraps = await EnsureProductWithInventoryAsync(
            suppApex.Id, catAccessory.Id,
            "SG-ACC-STRAPS-BLK",
            "SmartGym Pro Heavy Duty Lifting Straps",
            "Cotton-padded heavy duty lifting straps for enhanced grip during heavy pulls.",
            2800.00m, 1400.00m,
            quantityInStock: 45, reorderThreshold: 15, maxStock: 100, binLocation: "Aisle-3-C1",
            adminUserId: adminUser.Id);

        // 11. Initial Notifications
        await EnsureNotificationAsync(
            memberUser.Id,
            NotificationType.General,
            "Welcome to SmartGym!",
            "Welcome Nuwan! Your Gold Tier membership is active. Book your first class today.",
            "/member/dashboard");

        await EnsureNotificationAsync(
            memberUser.Id,
            NotificationType.Booking,
            "Class Booking Confirmed",
            "Your spot in 'Metabolic Blast HIIT' on Tuesday is confirmed.",
            "/member/bookings");

        _logger.LogInformation("SmartGym database seeding completed successfully.");
    }

    /// <summary>
    /// Ensures a Role exists in the database.
    /// </summary>
    private async Task<Role> EnsureRoleAsync(string name, string description)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        if (role == null)
        {
            role = new Role
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();
        }
        return role;
    }

    /// <summary>
    /// Ensures a User exists in the database with the specified role.
    /// </summary>
    private async Task<User> EnsureUserAsync(
        string email,
        string firstName,
        string lastName,
        string phone,
        string plainPassword,
        Role role)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword, 11),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow
            };
            await _context.UserRoles.AddAsync(userRole);
            await _context.SaveChangesAsync();
        }

        return user;
    }

    /// <summary>
    /// Ensures a Member profile exists for a given user.
    /// </summary>
    private async Task<Member> EnsureMemberProfileAsync(
        Guid userId,
        string emergencyContactName,
        string emergencyContactPhone,
        string gender,
        string address,
        string medicalConditions)
    {
        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member == null)
        {
            member = new Member
            {
                UserId = userId,
                EmergencyContactName = emergencyContactName,
                EmergencyContactPhone = emergencyContactPhone,
                Gender = gender,
                Address = address,
                MedicalConditions = medicalConditions,
                JoinDate = DateTime.UtcNow.AddMonths(-6),
                CreatedAt = DateTime.UtcNow
            };
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();
        }
        return member;
    }

    /// <summary>
    /// Ensures a MembershipPlan exists in the database.
    /// </summary>
    private async Task<MembershipPlan> EnsureMembershipPlanAsync(
        string name,
        string description,
        decimal price,
        int durationDays,
        int maxClassesPerWeek,
        bool hasTrainerAccess)
    {
        var plan = await _context.MembershipPlans.FirstOrDefaultAsync(p => p.Name == name);
        if (plan == null)
        {
            plan = new MembershipPlan
            {
                Name = name,
                Description = description,
                Price = price,
                DurationDays = durationDays,
                MaxClassesPerWeek = maxClassesPerWeek,
                HasTrainerAccess = hasTrainerAccess,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.MembershipPlans.AddAsync(plan);
            await _context.SaveChangesAsync();
        }
        return plan;
    }

    /// <summary>
    /// Ensures a Membership record exists for a member.
    /// </summary>
    private async Task EnsureMembershipAsync(Guid memberId, Guid planId, decimal pricePaid, int durationDays)
    {
        var existing = await _context.Memberships.FirstOrDefaultAsync(m => m.MemberId == memberId && m.Status == MembershipStatus.Active);
        if (existing == null)
        {
            var now = DateTime.UtcNow;
            var membership = new Membership
            {
                MemberId = memberId,
                PlanId = planId,
                StartDate = now.AddDays(-30),
                EndDate = now.AddDays(durationDays - 30),
                Status = MembershipStatus.Active,
                PricePaid = pricePaid,
                AutoRenew = false,
                CreatedAt = now.AddDays(-30)
            };
            await _context.Memberships.AddAsync(membership);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Ensures Goal and Progress records exist for a member.
    /// </summary>
    private async Task EnsureMemberGoalsAndProgressAsync(Guid memberId, Guid trainerId)
    {
        var existingGoal = await _context.Goals.FirstOrDefaultAsync(g => g.MemberId == memberId);
        if (existingGoal == null)
        {
            var goal1 = new Goal
            {
                MemberId = memberId,
                Title = "Achieve Bench Press 100kg",
                TargetValue = 100.00m,
                CurrentValue = 85.00m,
                Unit = "kg",
                TargetDate = DateTime.UtcNow.AddMonths(2),
                Status = GoalStatus.InProgress,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            };

            var goal2 = new Goal
            {
                MemberId = memberId,
                Title = "Body Fat Reduction to 12%",
                TargetValue = 12.00m,
                CurrentValue = 15.50m,
                Unit = "%",
                TargetDate = DateTime.UtcNow.AddMonths(3),
                Status = GoalStatus.InProgress,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            };

            await _context.Goals.AddRangeAsync(goal1, goal2);
            await _context.SaveChangesAsync();

            var record1 = new ProgressRecord
            {
                GoalId = goal1.Id,
                RecordedDate = DateTime.UtcNow.AddDays(-30),
                Value = 80.00m,
                Notes = "Good stability on eccentric phase. Increased from 75kg.",
                RecordedByTrainerId = trainerId,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var record2 = new ProgressRecord
            {
                GoalId = goal1.Id,
                RecordedDate = DateTime.UtcNow.AddDays(-5),
                Value = 85.00m,
                Notes = "Clean 3 reps at 85kg with spotter.",
                RecordedByTrainerId = trainerId,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            await _context.ProgressRecords.AddRangeAsync(record1, record2);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Ensures a Location exists in the database.
    /// </summary>
    private async Task<Location> EnsureLocationAsync(string name, string floor, string description)
    {
        var loc = await _context.Locations.FirstOrDefaultAsync(l => l.Name == name);
        if (loc == null)
        {
            loc = new Location
            {
                Name = name,
                Floor = floor,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Locations.AddAsync(loc);
            await _context.SaveChangesAsync();
        }
        return loc;
    }

    /// <summary>
    /// Ensures an Equipment record exists in the database.
    /// </summary>
    private async Task<Equipment> EnsureEquipmentAsync(
        Guid locationId,
        string serialNumber,
        string name,
        string model,
        string manufacturer,
        DateTime purchaseDate,
        EquipmentStatus status,
        DateTime lastServiced)
    {
        var eq = await _context.Equipment.FirstOrDefaultAsync(e => e.SerialNumber == serialNumber);
        if (eq == null)
        {
            eq = new Equipment
            {
                LocationId = locationId,
                SerialNumber = serialNumber,
                Name = name,
                Model = model,
                Manufacturer = manufacturer,
                PurchaseDate = purchaseDate,
                WarrantyExpiryDate = purchaseDate.AddYears(3),
                Status = status,
                LastServicedDate = lastServiced,
                CreatedAt = purchaseDate
            };
            await _context.Equipment.AddAsync(eq);
            await _context.SaveChangesAsync();
        }
        return eq;
    }

    /// <summary>
    /// Ensures a FacilityIssue and its related AIWorkflow exist.
    /// </summary>
    private async Task EnsureFacilityIssueAndAIWorkflowAsync(Guid memberId, Guid equipmentId, Guid locationId)
    {
        var issue = await _context.FacilityIssues.FirstOrDefaultAsync(fi => fi.EquipmentId == equipmentId);
        if (issue == null)
        {
            issue = new FacilityIssue
            {
                ReportedByMemberId = memberId,
                EquipmentId = equipmentId,
                LocationId = locationId,
                Title = "Motor belt slipping during speeds above 12 km/h",
                Description = "Treadmill belt stutters noticeably when accelerating past 12km/h, accompanied by a slight burnt rubber smell.",
                Severity = IssueSeverity.Medium,
                Status = FacilityIssueStatus.RequiresApproval,
                ReportedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            await _context.FacilityIssues.AddAsync(issue);
            await _context.SaveChangesAsync();

            var workflow = new AIWorkflow
            {
                IssueId = issue.Id,
                WorkflowType = "FacilityIssueDiagnosis",
                Status = AIWorkflowStatus.AwaitingApproval,
                CurrentStep = "CostEstimation",
                DiagnosisSummary = "Drive belt tensioner worn out; friction heating detected on belt ribs. Recommended replacement with OEM part LF-B882.",
                RecommendedAction = "Order OEM replacement belt LF-B882, lubricate front roller bearings, and calibrate belt tension.",
                EstimatedConfidenceScore = 0.94,
                RequiresHumanApproval = true,
                HumanApprovalGranted = null,
                TotalTokensUsed = 1840,
                ModelIdentifier = "gemini-1.5-pro",
                StructuredOutputPayloadJson = "{\"detectedPart\":\"Drive Belt\",\"suggestedPartNumber\":\"LF-B882\",\"estimatedPartCost\":14500.0,\"laborCost\":4500.0,\"estimatedTotalCost\":19000.0,\"estimatedDowntimeHours\":3}",
                StartedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            await _context.AIWorkflows.AddAsync(workflow);
            await _context.SaveChangesAsync();

            var step1 = new AIWorkflowStep
            {
                WorkflowId = workflow.Id,
                StepName = "IssueClassification",
                StepOrder = 1,
                Status = "Completed",
                Summary = "Issue classified as Mechanical - Drive Assembly Slippage with Medium Severity.",
                ExecutionDurationMs = 450,
                ExecutedAt = DateTime.UtcNow.AddDays(-2)
            };

            var step2 = new AIWorkflowStep
            {
                WorkflowId = workflow.Id,
                StepName = "CostAndPartAnalysis",
                StepOrder = 2,
                Status = "Completed",
                Summary = "Matched equipment model Elevation T95 to part LF-B882 with total estimate of 19,000 LKR.",
                ExecutionDurationMs = 820,
                ExecutedAt = DateTime.UtcNow.AddDays(-2)
            };

            await _context.AIWorkflowSteps.AddRangeAsync(step1, step2);
            await _context.SaveChangesAsync();

            var toolExec = new AIToolExecution
            {
                WorkflowStepId = step2.Id,
                ToolName = "EstimateRepairCost",
                InputParametersJson = "{\"model\":\"Elevation T95\",\"partName\":\"Drive Belt\",\"brand\":\"LifeFitness\"}",
                OutputResultJson = "{\"partNumber\":\"LF-B882\",\"partCost\":14500.0,\"avgLaborHours\":1.5,\"laborRate\":3000.0}",
                IsSuccess = true,
                ExecutionTimeMs = 380,
                ExecutedAt = DateTime.UtcNow.AddDays(-2)
            };
            await _context.AIToolExecutions.AddAsync(toolExec);

            var validation = new AIValidationResult
            {
                WorkflowId = workflow.Id,
                RuleName = "BudgetComplianceCheck",
                Passed = true,
                ValidationMessage = "Estimated cost 19,000 LKR is below automatic manager approval ceiling (25,000 LKR).",
                EvaluatedAt = DateTime.UtcNow.AddDays(-2)
            };
            await _context.AIValidationResults.AddAsync(validation);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Ensures a RepairOrder and its associated Approval exist.
    /// </summary>
    private async Task EnsureRepairOrderAndApprovalAsync(Guid equipmentId, Guid locationId, Guid memberId, Guid adminUserId)
    {
        var existingOrder = await _context.RepairOrders.FirstOrDefaultAsync(ro => ro.EquipmentId == equipmentId);
        if (existingOrder == null)
        {
            var issue = new FacilityIssue
            {
                ReportedByMemberId = memberId,
                EquipmentId = equipmentId,
                LocationId = locationId,
                Title = "Skillmill magnetic resistance lever jammed",
                Description = "Dual resistance lever stuck in maximum brake position; member unable to release tension.",
                Severity = IssueSeverity.High,
                Status = FacilityIssueStatus.InRepair,
                ReportedAt = DateTime.UtcNow.AddDays(-7),
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            };
            await _context.FacilityIssues.AddAsync(issue);
            await _context.SaveChangesAsync();

            var repairOrder = new RepairOrder
            {
                IssueId = issue.Id,
                EquipmentId = equipmentId,
                OrderNumber = "RO-2026-0041",
                EstimatedCost = 38000.00m,
                ActualCost = 36500.00m,
                Status = RepairOrderStatus.Approved,
                TechnicianName = "Sunil Wickramasinghe (Technogym Certified)",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };
            await _context.RepairOrders.AddAsync(repairOrder);
            await _context.SaveChangesAsync();

            var item1 = new RepairOrderItem
            {
                RepairOrderId = repairOrder.Id,
                PartName = "Technogym Skillmill Resistance Brake Cable Kit",
                PartNumber = "TG-SMC-CB-09",
                Quantity = 1,
                UnitCost = 28000.00m,
                TotalCost = 28000.00m
            };

            var item2 = new RepairOrderItem
            {
                RepairOrderId = repairOrder.Id,
                PartName = "Heavy Tension Calibration Spring",
                PartNumber = "TG-SMC-SP-02",
                Quantity = 2,
                UnitCost = 4250.00m,
                TotalCost = 8500.00m
            };

            await _context.RepairOrderItems.AddRangeAsync(item1, item2);

            var approval = new Approval
            {
                RepairOrderId = repairOrder.Id,
                ApproverUserId = adminUserId,
                Decision = ApprovalDecision.Approved,
                Comments = "High priority cardio unit required for peak hours. Approved under emergency maintenance fund.",
                ApprovalThreshold = 25000.00m,
                EstimatedCost = 38000.00m,
                DecidedAt = DateTime.UtcNow.AddDays(-5)
            };
            await _context.Approvals.AddAsync(approval);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Ensures a ClassCategory exists in the database.
    /// </summary>
    private async Task<ClassCategory> EnsureClassCategoryAsync(string name, string description)
    {
        var cat = await _context.ClassCategories.FirstOrDefaultAsync(c => c.Name == name);
        if (cat == null)
        {
            cat = new ClassCategory
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassCategories.AddAsync(cat);
            await _context.SaveChangesAsync();
        }
        return cat;
    }

    /// <summary>
    /// Ensures a FitnessClass exists in the database.
    /// </summary>
    private async Task<FitnessClass> EnsureFitnessClassAsync(
        Guid categoryId,
        string name,
        string description,
        int duration,
        int capacity,
        string intensity)
    {
        var fc = await _context.FitnessClasses.FirstOrDefaultAsync(c => c.Name == name);
        if (fc == null)
        {
            fc = new FitnessClass
            {
                CategoryId = categoryId,
                Name = name,
                Description = description,
                DurationMinutes = duration,
                DefaultCapacity = capacity,
                IntensityLevel = intensity,
                CreatedAt = DateTime.UtcNow
            };
            await _context.FitnessClasses.AddAsync(fc);
            await _context.SaveChangesAsync();
        }
        return fc;
    }

    /// <summary>
    /// Ensures ClassSchedules and Bookings exist.
    /// </summary>
    private async Task EnsureSchedulesAndBookingsAsync(
        Guid hiitClassId,
        Guid strengthClassId,
        Guid yogaClassId,
        Guid trainerUserId,
        Guid memberId)
    {
        var existingSchedule = await _context.ClassSchedules.FirstOrDefaultAsync(s => s.ClassId == hiitClassId);
        if (existingSchedule == null)
        {
            var today = DateTime.UtcNow.Date;
            var schedule1 = new ClassSchedule
            {
                ClassId = hiitClassId,
                TrainerId = trainerUserId,
                Room = "Studio 1 — High Intensity",
                StartTime = today.AddDays(1).AddHours(7),
                EndTime = today.AddDays(1).AddHours(7).AddMinutes(45),
                Capacity = 18,
                BookedCount = 1,
                Status = ScheduleStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            var schedule2 = new ClassSchedule
            {
                ClassId = strengthClassId,
                TrainerId = trainerUserId,
                Room = "Free Weights Area",
                StartTime = today.AddDays(2).AddHours(18),
                EndTime = today.AddDays(2).AddHours(19),
                Capacity = 15,
                BookedCount = 0,
                Status = ScheduleStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ClassSchedules.AddRangeAsync(schedule1, schedule2);
            await _context.SaveChangesAsync();

            var booking = new Booking
            {
                ScheduleId = schedule1.Id,
                MemberId = memberId,
                BookingTime = DateTime.UtcNow.AddHours(-12),
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow.AddHours(-12)
            };
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Ensures a Supplier exists in the database.
    /// </summary>
    private async Task<Supplier> EnsureSupplierAsync(
        string name,
        string contactPerson,
        string email,
        string phone,
        string address)
    {
        var s = await _context.Suppliers.FirstOrDefaultAsync(sup => sup.Name == name);
        if (s == null)
        {
            s = new Supplier
            {
                Name = name,
                ContactPerson = contactPerson,
                Email = email,
                Phone = phone,
                Address = address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Suppliers.AddAsync(s);
            await _context.SaveChangesAsync();
        }
        return s;
    }

    /// <summary>
    /// Ensures a ProductCategory exists in the database.
    /// </summary>
    private async Task<ProductCategory> EnsureProductCategoryAsync(string name, string description)
    {
        var c = await _context.ProductCategories.FirstOrDefaultAsync(cat => cat.Name == name);
        if (c == null)
        {
            c = new ProductCategory
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ProductCategories.AddAsync(c);
            await _context.SaveChangesAsync();
        }
        return c;
    }

    /// <summary>
    /// Ensures a Product with associated Inventory and StockMovement records exists.
    /// </summary>
    private async Task<Product> EnsureProductWithInventoryAsync(
        Guid supplierId,
        Guid categoryId,
        string sku,
        string name,
        string description,
        decimal unitPrice,
        decimal costPrice,
        int quantityInStock,
        int reorderThreshold,
        int maxStock,
        string binLocation,
        Guid adminUserId)
    {
        var product = await _context.Products
            .Include(p => p.InventoryItem)
            .FirstOrDefaultAsync(p => p.SKU == sku);

        if (product == null)
        {
            product = new Product
            {
                SupplierId = supplierId,
                CategoryId = categoryId,
                SKU = sku,
                Name = name,
                Description = description,
                UnitPrice = unitPrice,
                CostPrice = costPrice,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            var item = new InventoryItem
            {
                ProductId = product.Id,
                QuantityInStock = quantityInStock,
                ReorderThreshold = reorderThreshold,
                MaxStockLevel = maxStock,
                LocationBin = binLocation,
                LastRestockedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };
            await _context.InventoryItems.AddAsync(item);
            await _context.SaveChangesAsync();

            var movement = new StockMovement
            {
                InventoryItemId = item.Id,
                QuantityChange = quantityInStock,
                MovementType = StockMovementType.Restock,
                Reason = "Initial warehouse stock receipt from PO-2026-0105",
                PerformedByUserId = adminUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };
            await _context.StockMovements.AddAsync(movement);
            await _context.SaveChangesAsync();
        }

        return product;
    }

    private async Task EnsureNotificationAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string targetUrl)
    {
        var existing = await _context.Notifications.FirstOrDefaultAsync(n => n.UserId == userId && n.Title == title);
        if (existing == null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                TargetUrl = targetUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }
    }
}
