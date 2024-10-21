
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

    private:
        StringVector m_users;
        std::string m_name;
        bool m_active;
    };
}
