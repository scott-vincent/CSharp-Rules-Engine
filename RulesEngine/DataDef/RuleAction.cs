namespace RulesEngine.DataDef
{
    public class RuleAction
    {
        public enum ActionType
        {
            Field,
            FieldGroup,
            Block,
            Player
        }

        public ActionType Type { get; set; }
        public string Id { get; set; }

        public override string ToString()
        {
            return $"{Type.ToString()} {Id}";
        }
    }
}
