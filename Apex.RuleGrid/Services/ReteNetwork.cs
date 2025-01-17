using Apex.RuleGrid.Models;
using DocumentFormat.OpenXml.Presentation;

namespace Apex.RuleGrid.Services
{
    public class ReteNetwork
    {
        private List<Rule> _rules; // Holds the list of rules
        private readonly Dictionary<string, object> _factCache; // Cache for conditions and rules

        public ReteNetwork()
        {
            _rules = new List<Rule>();
            _factCache = new Dictionary<string, object>();
        }

        public void AddRuleSet(RuleSetDbModel ruleSet)
        {
            _rules.Clear(); // Clear existing rules
            _rules.AddRange(ruleSet.Rules); // Populate with new rules

            BuildFactCache(ruleSet);
        }

        private void BuildFactCache(RuleSetDbModel ruleSet)
        {
            _factCache.Clear();

            foreach (var rule in _rules.Where(x=>x.Index.Contains("#") == false))
            {
                foreach (var condition in rule.Conditions)
                {
                    var fieldName = RuleSetDbModel.GetRuleField(_rules, condition.Key, "#FieldName");
                    var operatorName = RuleSetDbModel.GetRuleField(_rules, condition.Key, "#Operator");
                    var factKey = $"{fieldName}:{operatorName}:{condition.Value}";
                    if (!_factCache.ContainsKey(factKey))
                    {
                        _factCache[factKey] = new List<Rule>();
                    }
                    (_factCache[factKey] as List<Rule>)?.Add(rule);
                }
            }
        }

        public IEnumerable<Rule> Match(Dictionary<string, object> facts, string conditionsOperator)
        {
            var matchedRules = new HashSet<Rule>();
            bool Match(Dictionary<string, object> facts, KeyValuePair<string, string> condition)
            {
                var fieldName = RuleSetDbModel.GetRuleField(_rules, condition.Key, "#FieldName");
                var operatorName = RuleSetDbModel.GetRuleField(_rules, condition.Key, "#Operator");
                switch (operatorName)
                {
                    case "Equals":
                        return facts[fieldName].ToString() == condition.Value;
                    case "GreaterThan":
                        return long.Parse(facts[fieldName].ToString()) > long.Parse(condition.Value);
                    case "LowerThan":
                        return long.Parse(facts[fieldName].ToString()) < long.Parse(condition.Value);
                    default:
                        return false;
                }
            }
            foreach (var rule in _rules.Where(x => x.Index.Contains("#") == false))
            {
                bool ruleMatches;

                if (conditionsOperator == "AND")
                {
                    ruleMatches = rule.Conditions.All(condition => Match(facts, condition));
                }
                else
                {
                    ruleMatches = rule.Conditions.Any(condition => Match(facts, condition));
                }

                if (ruleMatches)
                {
                    matchedRules.Add(rule);
                }
            }

            return matchedRules;
        }
    }

}
