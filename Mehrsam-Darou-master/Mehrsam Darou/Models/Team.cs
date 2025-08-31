using System;
using System.Collections.Generic;

namespace Mehrsam_Darou.Models;

public partial class Team
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? DefaultPageForTeam { get; set; }

    public bool? IsActive { get; set; }

    public bool? ManagmentDashboard { get; set; }

    public bool? Setting { get; set; }

    public bool? SystemUsers { get; set; }

    public bool? Financial { get; set; }

    public bool? Inventory { get; set; }

    public bool? Product { get; set; }

    public bool? SellCommercial { get; set; }

    public bool? BuyCommercial { get; set; }

    public bool? RandD { get; set; }

    public bool? Qc { get; set; }

    public bool? Qa { get; set; }

    public bool? Pmo { get; set; }

    public bool? ManagementDashboardDashboard { get; set; }

    public bool? ManagementDashboardNotifications { get; set; }

    public bool? ManagementDashboardAllRequests { get; set; }

    public bool? ManagementDashboardRequestsDashboard { get; set; }

    public bool? SystemUsersUserList { get; set; }

    public bool? SystemUsersTeamManagement { get; set; }

    public bool? HrAttendanceLog { get; set; }

    public bool? HrDailyAttendance { get; set; }

    public bool? HrSalaryManagement { get; set; }

    public bool? HrVacations { get; set; }

    public bool? HrVacationTypes { get; set; }

    public bool? HrSalaryCalculation { get; set; }

    public bool? ProductMedicines { get; set; }

    public bool? ProductMedicineCategories { get; set; }

    public bool? ProductRawMaterials { get; set; }

    public bool? ProductMaterialCategories { get; set; }

    public bool? ProductBom { get; set; }

    public bool? BuyCommercialSuppliers { get; set; }

    public bool? BuyCommercialPurchaseOrders { get; set; }

    public bool? BuyCommercialPurchaseInvoices { get; set; }

    public bool? InventoryStorageLocations { get; set; }

    public bool? InventoryMaterialBatches { get; set; }

    public bool? InventoryFinishedGoodsBatches { get; set; }

    public bool? PmoProductionOrders { get; set; }

    public bool? PmoProductionSteps { get; set; }

    public bool? RandDResearchProjects { get; set; }

    public bool? RandDDevelopment { get; set; }

    public bool? RandDFormulas { get; set; }

    public bool? QcQctests { get; set; }

    public bool? QcBatchTests { get; set; }

    public bool? QcQcreports { get; set; }

    public bool? QaQastandards { get; set; }

    public bool? QaQaaudits { get; set; }

    public bool? QaCertifications { get; set; }

    public bool? SellCommercialCustomers { get; set; }

    public bool? SellCommercialSalesOrders { get; set; }

    public bool? SellCommercialSalesInvoices { get; set; }

    public bool? SellCommercialShipments { get; set; }

    public bool? FinancialFinancialReports { get; set; }

    public bool? FinancialPayments { get; set; }

    public bool? FinancialAccounting { get; set; }

    public bool? CommunicationChat { get; set; }

    public bool? CommunicationMyNotifications { get; set; }

    public bool? CommunicationMyRequests { get; set; }

    public bool? SettingGeneralSettings { get; set; }

    public bool? SettingOrganizations { get; set; }

    public bool? SettingUnits { get; set; }

    public bool? SettingUnitTypes { get; set; }

    public bool? SettingPersianDateConverter { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
