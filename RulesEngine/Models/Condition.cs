namespace RulesEngine.Models
{
    public class Condition
    {
        public enum Oper
        {
            And,
            Or,
            NotDefined,
            IsDefined,
            Equal,
            NotEqual,
            LessThan,
            GreaterThan,
            LessThanOrEqual,
            GreaterThanOrEqual
        }

        public string Id { get; set; }
        public Oper Op { get; set; }
        public Value Value { get; set; }

        public override string ToString()
        {
            return $"{Id} {Op.ToString()} {Value}";
        }
    }
}
