
#include <string>
#include <vector>

namespace testapi {
    using StringVectorBase = std::vector<std::string>;
    class StringVector : public StringVectorBase
    {
    public:
        using StringVectorBase::StringVectorBase;
        StringVector() = default;
        StringVector(const StringVector& src) = default;
        StringVector& operator =(const testapi::StringVector&) = default;
        StringVector(StringVector&& src) = default;
        StringVector(const StringVectorBase& src) : StringVectorBase(src) { }
        StringVector(StringVectorBase&& src) : StringVectorBase(src) { }

        void Add(std::string_view item) { emplace_back(item); }
        std::string& Get(const size_t index) { return at(index); }
        size_t Size() { return size(); }
    };

    template<typename T>
    class TrivialTemplate
    {
    public:
        TrivialTemplate(T val) : m_Val(val) {}
        T& TemplateGetVal() { return m_Val; }

    private:
        T m_Val;
    };

    class UserGroup
    {
    public:
        UserGroup() = default;

        const StringVector& GetUsers() const { return m_users; }
        void SetUsers(const StringVector& users) { m_users = users; }

        std::string GetGroupName() const { return m_name; }
        void SetGroupName(const std::string_view groupName) { m_name = groupName; }

        bool GetGroupIsActive() const { return m_active; }
        void SetGroupIsActive(bool active) { m_active = active; }

        TrivialTemplate<bool> GetTemplateMember1() { return TrivialTemplate<bool>(m_active); }
        TrivialTemplate<int> GetTemplateMember2() { return TrivialTemplate<int>(m_index); }

        void InternalDoSomething() {}

    private:
        StringVector m_users;
        std::string m_name;
        int m_index = 0;
        bool m_active = false;
    };

    enum my_test_enum
    {
        my_test_enum_one,
        my_test_enum_two,
        my_test_enum_three,
    };

    enum class another_test_enum
    {
        foo,
        bar,
        bas
    };

    // This will help with the template linkage; note how these are not actually used in the UserGroup class directly
    using BoolTrivialTemplateBase = TrivialTemplate<bool>;
    class BoolTrivialTemplate : public BoolTrivialTemplateBase
    {
    public:
        BoolTrivialTemplate() = default;
        BoolTrivialTemplate(const BoolTrivialTemplate& src) = default;
        BoolTrivialTemplate& operator=(const BoolTrivialTemplate&) = default;
        BoolTrivialTemplate(BoolTrivialTemplate&& src) = default;
        explicit BoolTrivialTemplate(const BoolTrivialTemplateBase& src) :
            BoolTrivialTemplateBase(src) {
        }
        explicit BoolTrivialTemplate(BoolTrivialTemplateBase&& src) :
            BoolTrivialTemplateBase(std::move(src)) {
        }

        bool GetVal() { return TemplateGetVal(); }
    };

    using IntTrivialTemplateBase = TrivialTemplate<int>;
    class IntTrivialTemplate : public IntTrivialTemplateBase
    {
    public:
        IntTrivialTemplate() = default;
        IntTrivialTemplate(const IntTrivialTemplate& src) = default;
        IntTrivialTemplate& operator=(const IntTrivialTemplate&) = default;
        IntTrivialTemplate(IntTrivialTemplate&& src) = default;
        explicit IntTrivialTemplate(const IntTrivialTemplateBase& src) :
            IntTrivialTemplateBase(src) {
        }
        explicit IntTrivialTemplate(IntTrivialTemplateBase&& src) :
            IntTrivialTemplateBase(std::move(src)) {
        }

        int GetVal() { return TemplateGetVal(); }
    };
}
