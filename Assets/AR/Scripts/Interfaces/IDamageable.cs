public interface IDamageable
{
    // Any script that implements this interface MUST have a TakeDamage method
    void TakeDamage(float amount);
}