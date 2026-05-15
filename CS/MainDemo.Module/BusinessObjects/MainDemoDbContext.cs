using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.EFCore.DesignTime;
using DevExpress.ExpressApp.EFCore.Updating;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.Persistent.BaseImpl.EFCore.AuditTrail;
using Microsoft.EntityFrameworkCore;

namespace MainDemo.Module.BusinessObjects;

//This factory creates DbContext for design-time services. For example, it is required for database migration.
public class MainDemoDesignTimeDbContextFactory : DesignTimeDbContextFactory<MainDemoDbContext> {
    protected override string ConnectionString
        => throw new InvalidOperationException("Connection string not specified.");
}

[TypesInfoInitializer(typeof(DbContextTypesInfoInitializer<MainDemoDbContext>))]
public class MainDemoDbContext : DbContext {
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseDeferredDeletion(this);
        modelBuilder.SetOneToManyAssociationDeleteBehavior(DeleteBehavior.SetNull, DeleteBehavior.Cascade);
        modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);
        modelBuilder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);
        modelBuilder.UseOptimisticLock();

        modelBuilder.Entity<Paycheck>().Property(p => p.PayRate).HasPrecision(19, 4);
        modelBuilder.Entity<Paycheck>().Property(p => p.OvertimePayRate).HasPrecision(19, 4);

        modelBuilder.Entity<MediaDataObject>().HasOne(md => md.MediaResource).WithOne().HasForeignKey<MediaResourceObject>(p => p.ID);

        modelBuilder.Entity<AuditEFCoreWeakReference>()
            .HasMany(p => p.AuditItems)
            .WithOne(p => p.AuditedObject);
        modelBuilder.Entity<AuditEFCoreWeakReference>()
            .HasMany(p => p.OldItems)
            .WithOne(p => p.OldObject);
        modelBuilder.Entity<AuditEFCoreWeakReference>()
            .HasMany(p => p.NewItems)
            .WithOne(p => p.NewObject);
        modelBuilder.Entity<AuditEFCoreWeakReference>()
            .HasMany(p => p.UserItems)
            .WithOne(p => p.UserObject);

        modelBuilder.Entity<Department>()
            .HasMany(p => p.Employees)
            .WithOne(r => r.Department);
        modelBuilder.Entity<Department>()
            .HasOne(r => r.DepartmentHead);
        modelBuilder.Entity<DemoTask>()
            .HasOne(task => task.AssignedTo)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationUserLoginInfo>(b => {
            b.HasIndex(nameof(ISecurityUserLoginInfo.LoginProviderName), nameof(ISecurityUserLoginInfo.ProviderUserKey)).IsUnique();
        });
    }
    public MainDemoDbContext(DbContextOptions<MainDemoDbContext> options)
        : base(options) {
    }

    public DbSet<Address> Addresses { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Paycheck> Paychecks { get; set; }
    public DbSet<PortfolioFileData> FileAttachments { get; set; }
    public DbSet<FileData> FileData { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<ReportDataV2> ReportData { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<PhoneNumber> PhoneNumbers { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Resume> Resumes { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<DemoTask> Tasks { get; set; }
    public DbSet<DashboardData> DashboardData { get; set; }
    public DbSet<DynamicAppearanceRule> DynamicAppearanceRules { get; set; }

    #region Default XAF Configurations
    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<PermissionPolicyRole> Roles { get; set; }
    public DbSet<ApplicationUserLoginInfo> UserLoginsInfo { get; set; }
    public DbSet<ModelDifference> ModelDifferences { get; set; }
    public DbSet<ModelDifferenceAspect> ModelDifferenceAspects { get; set; }
    public DbSet<ModuleInfo> ModulesInfo { get; set; }
    public DbSet<AuditDataItemPersistent> AuditData { get; set; }
    public DbSet<AuditEFCoreWeakReference> AuditEFCoreWeakReferences { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }
    #endregion
}

public class AuditingDbContext : DbContext {
    public AuditingDbContext(DbContextOptions<AuditingDbContext> options)
        : base(options) {
    }

    public DbSet<AuditDataItemPersistent> AuditData { get; set; }
    public DbSet<AuditEFCoreWeakReference> AuditEFCoreWeakReferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);
        modelBuilder.Entity<AuditEFCoreWeakReference>()
            .HasMany(p => p.AuditItems)
            .WithOne(p => p.AuditedObject);
        modelBuilder.Entity<AuditEFCoreWeakReference>()
            .HasMany(p => p.OldItems)
            .WithOne(p => p.OldObject);
        modelBuilder.Entity<AuditEFCoreWeakReference>()
            .HasMany(p => p.NewItems)
            .WithOne(p => p.NewObject);
        modelBuilder.Entity<AuditEFCoreWeakReference>()
            .HasMany(p => p.UserItems)
            .WithOne(p => p.UserObject);
    }
}
