using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace CommunityBridge2
{
  public class RelayCommand : ICommand
  {
    public RelayCommand(Action<object> executeFunction)
    {
      _ExecuteFunction = executeFunction;
    }
    public RelayCommand(Action<object> executeFunction, Func<object, bool> canExecuteFunction)
    {
      _ExecuteFunction = executeFunction;
      _CanExecuteFunction = canExecuteFunction;
    }

    private bool? _CanExecute;
    public void ChangeCanExecute(bool newValue)
    {
      if (_CanExecute != newValue)
      {
        _CanExecute = newValue;
        RaiseCanExecuteChanged();
      }
    }
    public void ChangeCanExecute()
    {
      RaiseCanExecuteChanged();
    }

    private Func<object, bool> _CanExecuteFunction;
    public virtual bool CanExecute(object parameter)
    {
      if (_CanExecute.HasValue)
      {
        return _CanExecute.Value;
      }
      if (_CanExecuteFunction != null)
      {
        return _CanExecuteFunction(parameter);
      }
      return true;
    }

    public event EventHandler CanExecuteChanged;
    private void RaiseCanExecuteChanged()
    {
      if (CanExecuteChanged != null)
      {
        CanExecuteChanged(this, EventArgs.Empty);
      }
    }

    private Action<object> _ExecuteFunction;
    public virtual void Execute(object parameter)
    {
      if (_ExecuteFunction != null)
      {
        _ExecuteFunction(parameter);
      }
    }
  }  // class RelayCommand

  public class RelayContextCommand<T> : RelayCommand
  {
    public RelayContextCommand(Action<T, object> executeFunction)
      : base(null)
    {
      this.executeFunction = executeFunction;
    }
    public RelayContextCommand(Action<T, object> executeFunction, Func<T, object, bool> canExecuteFunction)
      : base(null, null)
    {
      this.executeFunction = executeFunction;
      this.canExecuteFunction = canExecuteFunction;
    }

    public T Context { get; set; }

    Action<T, object> executeFunction;
    Func<T, object, bool> canExecuteFunction;

    public override bool CanExecute(object parameter)
    {
      if (canExecuteFunction == null)
        return base.CanExecute(parameter);
      return canExecuteFunction(Context, parameter);
    }

    public override void Execute(object parameter)
    {
      if (executeFunction == null)
        base.Execute(parameter);
      executeFunction(Context, parameter);
    }
  }
}  // namespace HMBase.WPF
