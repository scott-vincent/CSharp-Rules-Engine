using RulesEngine.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Models
{
    public abstract class Value
    {
        public abstract object GetValue();
        public abstract bool CompareValue(Condition.Oper op, Value value);
        public override abstract string ToString();
    }

    public class StringValue : Value
    {
        private string _value;

        public StringValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _value = null;
            }
            else
            {
                _value = value;
            }
        }

        public override object GetValue()
        {
            return _value;
        }

        public override bool CompareValue(Condition.Oper op, Value value)
        {
            switch (op)
            {
                case Condition.Oper.Equal:
                    return _value == (string)value.GetValue();
                case Condition.Oper.NotEqual:
                    return _value != (string)value.GetValue();
                default:
                    throw new RulesException("Unknown string operation");
            }
        }

        public override string ToString()
        {
            return _value;
        }
    }

    public class IntValue : Value
    {
        private int? _value;

        public IntValue(int value)
        {
            _value = value;
        }

        public IntValue(string value)
        {
            try
            {
                _value = int.Parse(value);
            }
            catch (Exception)
            {
                _value = null;
            }
        }

        public override object GetValue()
        {
            return _value;
        }

        public override bool CompareValue(Condition.Oper op, Value value)
        {
            switch (op)
            {
                case Condition.Oper.Equal:
                    return _value == (int)value.GetValue();
                case Condition.Oper.NotEqual:
                    return _value != (int)value.GetValue();
                case Condition.Oper.LessThan:
                    return _value < (int)value.GetValue();
                case Condition.Oper.GreaterThan:
                    return _value > (int)value.GetValue();
                case Condition.Oper.LessThanOrEqual:
                    return _value <= (int)value.GetValue();
                case Condition.Oper.GreaterThanOrEqual:
                    return _value >= (int)value.GetValue();
                default:
                    throw new RulesException("Unknown int operation");
            }
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }

    public class DecimalValue : Value
    {
        private decimal? _value;

        public DecimalValue(decimal value)
        {
            _value = value;
        }

        public DecimalValue(string value)
        {
            try
            {
                _value = decimal.Parse(value);
            }
            catch (Exception)
            {
                _value = null;
            }
        }

        public override object GetValue()
        {
            return _value;
        }

        public override bool CompareValue(Condition.Oper op, Value value)
        {
            switch (op)
            {
                case Condition.Oper.Equal:
                    return _value == (decimal)value.GetValue();
                case Condition.Oper.NotEqual:
                    return _value != (decimal)value.GetValue();
                case Condition.Oper.LessThan:
                    return _value < (decimal)value.GetValue();
                case Condition.Oper.GreaterThan:
                    return _value > (decimal)value.GetValue();
                case Condition.Oper.LessThanOrEqual:
                    return _value <= (decimal)value.GetValue();
                case Condition.Oper.GreaterThanOrEqual:
                    return _value >= (decimal)value.GetValue();
                default:
                    throw new RulesException("Unknown decimal operation");
            }
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }

    public class BoolValue : Value
    {
        private bool? _value;

        public BoolValue(bool value)
        {
            _value = value;
        }

        public BoolValue(string value)
        {
            switch (value.ToLower())
            {
                case "true":
                    _value = true;
                    break;
                case "false":
                    _value = false;
                    break;
                default:
                    _value = null;
                    break;
            }
        }

        public override object GetValue()
        {
            return _value;
        }

        public override bool CompareValue(Condition.Oper op, Value value)
        {
            switch (op)
            {
                case Condition.Oper.Equal:
                    return _value == (bool)value.GetValue();
                case Condition.Oper.NotEqual:
                    return _value != (bool)value.GetValue();
                default:
                    throw new RulesException("Unknown bool operation");
            }
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
