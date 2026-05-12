using System.ComponentModel;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace MainDemo.Module.BusinessObjects;

[DefaultClassOptions]
[DefaultProperty(nameof(Employee))]
[RuleCriteria("Payroll_Hours_PayPeriod_Range", DefaultContexts.Save, "DateDiffHour(PayPeriodStart, PayPeriodEnd) >= [Hours] + [OvertimeHours]", CustomMessageTemplate = @"Sum of ""Hours"" and ""Overtime hours"" must be less than or equal to the difference between ""Pay Period End"" and ""Pay Period Start"" in hours.")]
public class Paycheck : BaseObject, INotifyPropertyChanged {
    private Employee employee;
    private int payPeriod;
    private DateTime paymentDate;
    private DateTime payPeriodEnd;
    private DateTime payPeriodStart;
    private decimal payRate;
    private int hours;
    private decimal overtimePayRate;
    private int overtimeHours;
    private double taxRate;
    private string notes;

    [RuleRequiredField]
    [ImmediatePostData]
    public virtual Employee Employee {
        get { return employee; }
        set { SetPropertyValue(ref employee, value); }
    }

    [RuleRange(DefaultContexts.Save, 0, 26)]
    public virtual int PayPeriod {
        get { return payPeriod; }
        set { SetPropertyValue(ref payPeriod, value); }
    }

    [RuleRequiredField]
    public virtual DateTime PayPeriodStart {
        get { return payPeriodStart; }
        set { SetPropertyValue(ref payPeriodStart, value); }
    }

    [RuleRequiredField]
    [RuleValueComparison("Payroll_PeriodStart_PeriodEnd", DefaultContexts.Save, ValueComparisonType.GreaterThanOrEqual, nameof(PayPeriodStart), ParametersMode.Expression)]
    [RuleValueComparison("Payroll_PaymentDate_PeriodEnd", DefaultContexts.Save, ValueComparisonType.GreaterThanOrEqual, nameof(PaymentDate), ParametersMode.Expression)]
    public virtual DateTime PayPeriodEnd {
        get { return payPeriodEnd; }
        set { SetPropertyValue(ref payPeriodEnd, value); }
    }

    public virtual DateTime PaymentDate {
        get { return paymentDate; }
        set { SetPropertyValue(ref paymentDate, value); }
    }

    [ImmediatePostData]
    [RuleValueComparison("Payroll_PayRate", DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual decimal PayRate {
        get { return payRate; }
        set {
            if(SetPropertyValue(ref payRate, value))
                NotifyCalculatedPropertiesChanged();
        }
    }

    [ImmediatePostData]
    [RuleValueComparison("Payroll_Hours", DefaultContexts.Save, ValueComparisonType.GreaterThanOrEqual, 0)]
    public virtual int Hours {
        get { return hours; }
        set {
            if(SetPropertyValue(ref hours, value))
                NotifyCalculatedPropertiesChanged();
        }
    }

    [ImmediatePostData]
    [RuleValueComparison("Payroll_OvertimePayRate", DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual decimal OvertimePayRate {
        get { return overtimePayRate; }
        set {
            if(SetPropertyValue(ref overtimePayRate, value))
                NotifyCalculatedPropertiesChanged();
        }
    }

    [ImmediatePostData]
    [RuleValueComparison("Payroll_OvertimeHours", DefaultContexts.Save, ValueComparisonType.GreaterThanOrEqual, 0)]
    public virtual int OvertimeHours {
        get { return overtimeHours; }
        set {
            if(SetPropertyValue(ref overtimeHours, value))
                NotifyCalculatedPropertiesChanged();
        }
    }

    [ImmediatePostData]
    [RuleRange(DefaultContexts.Save, 0, 100)]
    public virtual double TaxRate {
        get { return taxRate; }
        set {
            if(SetPropertyValue(ref taxRate, value))
                NotifyCalculatedPropertiesChanged();
        }
    }

    [FieldSize(4096)]
    public virtual string Notes {
        get { return notes; }
        set { SetPropertyValue(ref notes, value); }
    }

    private void NotifyCalculatedPropertiesChanged() {
        OnPropertyChanged(nameof(TotalTax));
        OnPropertyChanged(nameof(GrossPay));
        OnPropertyChanged(nameof(NetPay));
    }

    [PersistentAlias("((PayRate * Hours) + (OvertimePayRate * OvertimeHours)) * TaxRate")]
    public decimal TotalTax {
        get => EvaluateAlias<decimal>();
    }

    [PersistentAlias("(PayRate * Hours) + (OvertimePayRate * OvertimeHours)")]
    public decimal GrossPay {
        get => EvaluateAlias<decimal>();
    }

    [PersistentAlias("GrossPay - TotalTax")]
    public decimal NetPay {
        get => EvaluateAlias<decimal>();
    }

    public override void OnCreated() {
        DateTime now = DateTime.Now;
        payPeriod = (2 * (now.Month - 1)) + (now.Day > 15 ? 2 : 1);
        payPeriodStart = new DateTime(day: 1, month: now.Month, year: now.Year);
        payPeriodEnd = new DateTime(day: (now.Day > 15 ? DateTime.DaysInMonth(now.Year, now.Month) : 15), month: now.Month, year: now.Year);
        paymentDate = this.payPeriodEnd;
    }

    #region INotifyPropertyChanged

    private PropertyChangedEventHandler propertyChanged;
    protected bool SetPropertyValue<T>(ref T propertyValue, T newValue, [CallerMemberName] string propertyName = null) {
        if(EqualityComparer<T>.Default.Equals(propertyValue, newValue)) {
            return false;
        }
        propertyValue = newValue;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
        if(propertyChanged != null) {
            propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
        add { propertyChanged += value; }
        remove { propertyChanged -= value; }
    }

    #endregion
}
